using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using libtcod;
using RogueGame.Generation;
using RogueGame.Components;

namespace RogueGame.Map {

    /// <summary>
    /// Arbitrary tile space that handles terrain types and visiblity b/w tiles etc.
    /// Can represent exterior open spaces as well as interior dungeon type setups
    /// </summary>
    [Serializable()]
    public class AreaMap: ISerializable {

        [Serializable()]
        public struct Tile : ISerializable {
            public Terrain terrain;

            // TODO: use int flags if it starts getting to be lots of, well, flags
            public bool cachedLOS;
            public bool explored;

            // all entities on this tile
            public List<Entity> entities;

            public Tile(Terrain terrainType) {
                this.terrain = terrainType;
                cachedLOS = false;
                explored = false;
                entities = new List<Entity>(1);
            }

            // Tile also needs custom serialization to be able to properly handle terrain objects
            public void GetObjectData(SerializationInfo info, StreamingContext context) {
                info.AddValue("cachedLOS", cachedLOS, typeof(bool));
                info.AddValue("explored", explored, typeof(bool));
                info.AddValue("terrain", terrain.name, typeof(string));
            }

            public Tile(SerializationInfo info, StreamingContext context) {
                cachedLOS = (bool)info.GetValue("cachedLOS", typeof(bool));
                explored = (bool)info.GetValue("explored", typeof(bool));
                entities = new List<Entity>();
                string terrainType = (string)info.GetValue("terrain", typeof(string));

                terrain = Engine.instance.world.currentMapInfo.GetTerrain(terrainType);
            }
        }


        /// <summary>
        /// Terrain that is used for any unexplored tiles
        /// </summary>
        public static Terrain unexplored = new Terrain("Unexplored", Terrain.EMobilityLevel.BlocksMove,
                                new TCODColor(54, 73, 92), new TCODColor(59, 79, 99), '#');

        public string areaName;
        public int width;
        public int height;

        public Vector2 entranceLoc;
        public Vector2 exitLoc;

        /// <summary>
        /// Main tile data structure that is width * height in size
        /// </summary>
        private Tile[] tiles;

        /// <summary>
        /// List of all objects added to the level
        /// </summary>
        public List<Entity> levelEntities;
        
        private World parentWorld;

        public AreaMap(World parentWorld, int width, int height, Generator terrainGenerator) {
            this.parentWorld = parentWorld;
            this.width = width;
            this.height = height;

            areaName = "Test Map";
            
            // Init the set of tiles as blocking so that they can be "carved" out to make rooms etc
            tiles = new Tile[width * height];
            for(int i = 0; i < tiles.Length; i++) {
                tiles[i] = new Tile(terrainGenerator.GetTerrainType(Terrain.EMobilityLevel.BlocksMove));
            }

            levelEntities = new List<Entity>();
            
            terrainGenerator.GenerateMap(tiles, width, height);
        }


        // Randomly selects an open tile as the spawn location
        public Vector2 ChooseRandomLocation() {
            TCODRandom rng = TCODRandom.getInstance();
            Vector2 randomLoc = new Vector2(rng.getInt(0, width - 1), rng.getInt(0, height - 1));

            return GetNearestValidMove(randomLoc);
        }


        public Tile GetTile(int x, int y) {
            return tiles[x + y * width];
        }

        public Tile GetTile(Vector2 pos) {
            return GetTile(pos.X, pos.Y);
        }


        /// <summary>
        /// Determines what character to use for the given location of terrain
        /// </summary>
        public char GetChar(int x, int y) {
            Tile t = GetTile(x, y);

            return (char)t.terrain.ch;
        }


        /// <summary>
        /// Returns true if the terrain is passable. Only checks for entites if requested
        /// </summary>
        public bool CanMoveTo(int x, int y, bool checkEntities = false) {
            // Simplest check is if its in bounds of the map
            if(x < 0 || x >= width || y < 0 || y >= height) {
                return false;
            }

            Tile t = GetTile(x, y);
            if(t.terrain.mobility == Terrain.EMobilityLevel.BlocksMove) {
                return false;
            }
            
            if(checkEntities) {
                // check if there is an entity on the tile that obscures los
                EGetBlockState blocks;
                foreach(var e in GetTile(x, y).entities) {
                    blocks = (EGetBlockState)e.FireEvent(new EGetBlockState());
                    if(blocks.blocking) {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Move entity e onto tile x,y
        /// </summary>
        public bool PutOnTile(Entity e, int x, int y) {
            if(!CanMoveTo(x, y)) {
                return false;
            }

            Tile t = GetTile(x, y);
            t.entities.Add(e);
            return true;
        }

        /// <summary>
        /// Moves the entity onto the target tile, does not check mvoe validity
        /// </summary>
        public void MoveEntity(Entity e, Vector2 target) {
            GetTile(e.position.X, e.position.Y).entities.Remove(e);
            GetTile(target.X, target.Y).entities.Add(e);
            e.position = target;
        }

        /// <summary>
        /// Takes the entity off of whatever tiles its on
        /// </summary>
        public void RemoveFromTile(Entity e) {
            if(InBounds(e.position.X, e.position.Y)) {
                GetTile(e.position.X, e.position.Y).entities.Remove(e);
            }            
        }



        /// <summary>
        /// Checks if any of the objects in the level manager are at this position
        /// </summary>
        public Entity GetObjectAt(int x, int y) {
            Tile t = GetTile(x, y);
            return t.entities.Count > 0? t.entities[0] : null;
        }

        /// <summary>
        /// Gets all the objects from the level manager that are at this coordinate
        /// </summary>
        public Entity[] GetAllObjectsAt(int x, int y) {
            Tile t = GetTile(x, y);
            return t.entities.ToArray();
        }

        /// <summary>
        /// Gets all of the entities within the radius of the input position
        /// </summary>
        public Entity[] GetAllObjectsInRadius(Vector2 target, float radius) {
            List<Entity> output = new List<Entity>();
            foreach(Entity e in levelEntities) {
                float dist = Vector2.Distance(target, e.position);

                if(dist < radius || (radius == 0.0f && target == e.position)) {
                    output.Add(e);
                }
            }

            return output.ToArray();
        }

        
        /// <summary>
        /// Updates all tiles chached LOS from the input location
        /// </summary>
        public void UpdateLOSFrom(Vector2 from, int maxRange = 0) {
            Vector2 tilePos;
            for(int x = 0; x < width; x++) {
                for(int y = 0; y < height; y++) {
                    tilePos.x = x;
                    tilePos.y = y;
                    tiles[x + y * width].cachedLOS = HasLOS(from, tilePos, maxRange);

                    // Also, if the tile hasn;t been explored, explore it now
                    if(!tiles[x + y * width].explored) {
                        tiles[x + y * width].explored = tiles[x + y * width].cachedLOS;
                    }
                }
            }
        }


        /// <summary>
        /// Does a breadth first search out from the startPos, until it finds an open tile
        /// </summary>
        public Vector2 GetNearestValidMove(Vector2 startPos) {
            Vector2 testPos = new Vector2(startPos);

            // List of directions to search in from each tile - is just the cardinal directions
            Vector2[] checkDirection = new Vector2[] { new Vector2(0, 1), new Vector2(0, -1),
                                                       new Vector2(1, 0), new Vector2(-1, 0) };

            // Simple depth first flood fill sort of search                                                      
            Queue<Vector2> searchSpace = new Queue<Vector2>();
            List<Vector2> searched = new List<Vector2>();

            searchSpace.Enqueue(startPos);
            searched.Add(startPos);

            while(searchSpace.Count > 0) {
                Vector2 nextPos = searchSpace.Dequeue();

                // check if this tile is open
                if(CanMoveTo(nextPos.X, nextPos.Y, true)) {
                    return nextPos;
                }

                // otherwise, do a depth first search outwards
                for(int i = 0; i < checkDirection.Length; i++) {
                    Vector2 neighbor = nextPos + checkDirection[i];

                    if(!searched.Contains(neighbor) && InBounds(neighbor.X, neighbor.Y)) {
                        searchSpace.Enqueue(neighbor);
                        searched.Add(neighbor);
                    }
                }
            }

            return testPos;
        }


        /// <summary>
        /// Checks if the tile is relevant to the player right now
        /// </summary>
        public bool IsInPlayersLOS(int x, int y) {
            return GetTile(x, y).cachedLOS;
        }

        public bool IsInPlayersLOS(Vector2 pos) {
            return GetTile(pos.X, pos.Y).cachedLOS;
        }


        /// <summary>
        /// Checks if to can be seen from the position from
        /// </summary>
        public bool HasLOS(Vector2 from, Vector2 to, int maxRange = 0) {
            // check that both coords are in bounds
            if(!InBounds(from.X, from.Y) || !InBounds(to.X, to.Y)) {
                return false;
            }

            // Easy out if they are in the same location
            if(from == to) {
                return true;
            }

            Vector2 delta = from - to;
            int absDeltax = Math.Abs(delta.X);
            int absDeltay = Math.Abs(delta.Y);
            int signx = Math.Sign(delta.x);
            int signy = Math.Sign(delta.y);
            int error = 0;

            int x = to.X;
            int y = to.Y;

            // Check that this is in range
            if(maxRange != 0 && (delta.SqrMagnitude() > maxRange * maxRange)) {
                return false;
            }

            // Is the line x or y dominant
            if(absDeltax > absDeltay) {
                error = absDeltay * 2 - absDeltax;
                do {
                    if(error >= 0) {
                        y += signy;
                        error -= absDeltax * 2;
                    }

                    x += signx;
                    error += absDeltay * 2;

                    if(x == from.x && y == from.y) {
                        return true;
                    }
                } while(!BlocksSight(x, y));

                return false;
            }
            else {
                error = absDeltax * 2 - absDeltay;
                do {
                    if(error >= 0) {
                        x += signx;
                        error -= absDeltay * 2;
                    }

                    y += signy;
                    error += absDeltax * 2;

                    if(x == from.x && y == from.y) {
                        return true;
                    }
                } while(!BlocksSight(x, y));

                return false;
            }
        }


        /// <summary>
        /// walks from 'from' to 'to' until it reaches a position that is non passable.
        /// If there is no obstruction on  line b/w the two points, it return the position
        /// of 'to'.
        /// also passes the normal of the collision, or zero vectro if non occured
        /// </summary>
        public Vector2 CollisionPointFromTo(Vector2 from, Vector2 to, out Vector2 collisionNormal) {
            collisionNormal = Vector2.Zero;

            if(from == to) {
                return to;
            }

            Vector2 delta = to - from;
            float maxDist = delta.Magnitude();
            int absDeltax = Math.Abs(delta.X);
            int absDeltay = Math.Abs(delta.Y);
            int signx = Math.Sign(delta.x);
            int signy = Math.Sign(delta.y);
            int error = 0;

            int x = from.X;
            int y = from.Y;

            // Just setup the normal by default, so it only gets written once
            if(absDeltax >= absDeltay) {
                collisionNormal = new Vector2(signx, 0);
            }
            else {
                collisionNormal = new Vector2(0, signy);
            }

            // Is the line x or y dominant
            if(absDeltax > absDeltay) {
                error = absDeltay * 2 - absDeltax;

                int prevX, prevY;
                do {
                    prevX = x;
                    prevY = y;

                    if(error >= 0) {
                        y += signy;
                        error -= absDeltax * 2;
                    }

                    x += signx;
                    error += absDeltay * 2;

                    float dist = Vector2.Distance(from, new Vector2(x, y));

                    if(x == to.X && y == to.Y || dist >= maxDist) {
                        if(CanMoveTo(to.X, to.Y)) {
                            return to;
                        }
                        else {
                            return new Vector2(prevX, prevY);
                        }
                    }

                } while(!BlocksSight(x, y));

                if(CanMoveTo(x, y)) {
                    return new Vector2(x, y);
                }
                else {
                    return new Vector2(prevX, prevY);
                }
            }
            else {
                error = absDeltax * 2 - absDeltay;

                int prevX, prevY;
                do {
                    prevX = x;
                    prevY = y;

                    if(error >= 0) {
                        x += signx;
                        error -= absDeltay * 2;
                    }

                    y += signy;
                    error += absDeltax * 2;

                    float dist = Vector2.Distance(from, new Vector2(x, y));

                    if(x == to.X && y == to.Y || dist >= maxDist) {
                        if(CanMoveTo(to.X, to.Y)) {
                            return to;
                        }
                        else {
                            return new Vector2(prevX, prevY);
                        }
                    }

                } while(!BlocksSight(x, y));

                if(CanMoveTo(x, y)) {
                    return new Vector2(x, y);
                }
                else {
                    return new Vector2(prevX, prevY);
                }
            }

        }


        /// <summary>
        /// Checks if the input location blocks LOS through it
        /// </summary>
        public bool BlocksSight(int x, int y) {
            Terrain.EMobilityLevel m = GetTile(x, y).terrain.mobility;
            if(m == Terrain.EMobilityLevel.BlocksMove ||
                    m == Terrain.EMobilityLevel.BlocksVisilbity) {
                return true;
            }

            // check if there is an entity on the tile that obscures los
            EGetBlocksLOS checkLOS;
            foreach(var e in GetTile(x, y).entities) {
                checkLOS = (EGetBlocksLOS)e.FireEvent(new EGetBlocksLOS());
                if(checkLOS.blocksLOS) {
                    return true;
                }
            }

            return false;
        }

        public bool InBounds(Vector2 pos) {
            return InBounds(pos.X, pos.Y);
        }

        /// <summary>
        /// Checks if the input location is withing the tiles array bounds
        /// </summary>
        public bool InBounds(int x, int y) {
            if(x < 0 || x >= width || y < 0 || y >= height) {
                return false;
            }

            return true;
        }






        // =================================================================================================================================================================================================================
        // Serialization handling: 
        // =================================================================================================================================================================================================================
        


        // Implement this method to serialize data. The method is called on serialization.
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("width", width, typeof(int));
            info.AddValue("height", height, typeof(int));
            info.AddValue("entranceLoc", entranceLoc, typeof(Vector2));
            info.AddValue("exitLoc", exitLoc, typeof(Vector2));

            info.AddValue("tiles", tiles, typeof(Tile[]));
            
            info.AddValue("levelEntities", levelEntities, typeof(List<Entity>));
        }



        // The special constructor is used to deserialize values.
        public AreaMap(SerializationInfo info, StreamingContext context) {
            parentWorld = Engine.instance.world;

            width = (int)info.GetValue("width", typeof(int));
            height = (int)info.GetValue("height", typeof(int));
            entranceLoc = (Vector2)info.GetValue("entranceLoc", typeof(Vector2));
            exitLoc = (Vector2)info.GetValue("exitLoc", typeof(Vector2));

            levelEntities = (List<Entity>)info.GetValue("levelEntities", typeof(List<Entity>));            
            tiles = (Tile[])info.GetValue("tiles", typeof(Tile[]));
        }


        public void PostDeserializeInit() {
            foreach(Entity e in levelEntities) {
                Tile t = GetTile(e.position);
                t.entities.Add(e);
            }
        }



    }
}
