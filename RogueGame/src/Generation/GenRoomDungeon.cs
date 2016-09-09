using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;
using RogueGame.Data;

namespace RogueGame.Generation {

    public class GenRoomDungeon : Generator {
        
        private const int   MIN_ROOM_SIZE       = 6;
        private const int   MAX_ROOM_SIZE       = 12;
        private const float MAX_SIDE_RATIO      = 1.5f;
        private const int   NUM_SUBDIVISIONS    = 8;

        private const float  DOOR_RATE          = 0.25f;

        /// <summary>
        /// Simple representation of a rectangle
        /// </summary>
        private class Rect {
            public int x = 0;
            public int y = 0;
            public int w = 0;
            public int h = 0;

            public Rect(int x, int y, int w, int h) {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }

            public int Area() { return w * h; }
            public int centerX() { return x + w / 2; }
            public int centerY() { return y + h / 2; }
        }

        /// <summary>
        /// Contains the data about the room positiona nd size, and also it's 
        /// neighboring rooms to create a graph
        /// </summary>
        private class DungeonRoom {
            public Rect data;
            
            public List<DungeonRoom> neighbors;
            
            public DungeonRoom(Rect rect) {
                data = rect;
                neighbors = new List<DungeonRoom>(1);
            }
        }

        /// <summary>
        /// The handler that will generate the rooms from teh bsp tree
        /// </summary>
        private class RoomGenerator : ITCODBspCallback {
            private TCODRandom rng;

            // list of created rooms
            public List<DungeonRoom> rooms;

            public RoomGenerator(TCODRandom rng) {
                this.rng = rng;
                rooms = new List<DungeonRoom>();
            }

            public override bool visitNode(TCODBsp node) {
                if(!node.isLeaf()) {
                    return true;
                }

                int w = rng.getInt(MIN_ROOM_SIZE, node.w - 2);
                int h = rng.getInt(MIN_ROOM_SIZE, node.h - 2);
                int x = rng.getInt(node.x + 1, node.x + node.w - w - 1);
                int y = rng.getInt(node.y + 1, node.y + node.h - h - 1);

                Rect rect = new Rect(x, y, w, h);
                rooms.Add(new DungeonRoom(rect));
                return true;
            }
        }

        // cache map size
        private int width;
        private int height;

        public GenRoomDungeon(TCODRandom rng, Terrain[] terrains, MapInfo mapInfo) 
            : base(rng, terrains, mapInfo) {

        }


        public override void GenerateMap(AreaMap.Tile[] tiles, int width, int height) {
            this.width = width;
            this.height = height;

            objectSpawns = new List<ObjectSpawn>();

            // generate a binary tree of rooms that subdivides the space, suignthe TCODBsp module
            TCODBsp bsp = new TCODBsp(0, 0, width, height);
            RoomGenerator roomGenerator = new RoomGenerator(rng);
            bsp.splitRecursive(rng, NUM_SUBDIVISIONS, MAX_ROOM_SIZE, MAX_ROOM_SIZE, MAX_SIDE_RATIO, MAX_SIDE_RATIO);            
            bsp.traverseInvertedLevelOrder(roomGenerator);
            
            // dig out the generated rooms
            foreach(DungeonRoom room in roomGenerator.rooms) {
                DigArea(tiles, room.data.x, room.data.y, room.data.x + room.data.w - 1, room.data.y + room.data.h - 1);
            }

            // generate a sparsely connected graph of the created rooms, so that all rooms are connected
            CreateSparseRoomGraph(roomGenerator.rooms);

            // follow the connections in the graph and generate a tunnel b/w the rooms that are connected
            foreach(DungeonRoom room in roomGenerator.rooms) {
                // dig a hallway to each neighboring room
                foreach(DungeonRoom neighbor in room.neighbors) {
                    Rect roomData = room.data;
                    Rect nextData = neighbor.data;

                    ConnectRooms(tiles, roomData, nextData);
                }
            }
            
            // decide where the player should spawn
            int playerRoomIdx = rng.getInt(0, roomGenerator.rooms.Count - 1);
            DungeonRoom playerRoom = roomGenerator.rooms[playerRoomIdx];

            Vector2 playerLoc = RandomSpawnInRoom(playerRoom);
            objectSpawns.Add(new ObjectSpawn(playerLoc, ENTRANCE_ENTITY));
            objectSpawns.Add(new ObjectSpawn(playerLoc, "Player"));

            // place doors between rooms where appropriate, and spawn other entities and obejcts as well
            int numChests = 0;
            foreach(DungeonRoom room in roomGenerator.rooms) {
                GetDoors(tiles, room.data);

                // decide if any mobs should go in this room: defs not if its the start room
                if(room == playerRoom) {
                    continue;
                }

                SpawnContainers(room, mapInfo, ref numChests);
                SpawnMobs(room, mapInfo);
            }

            // spawn a key for each chest
            for(int i = 0; i < numChests; i++) {
                Vector2 loc = RandomSpawn(roomGenerator.rooms);
                objectSpawns.Add(new ObjectSpawn(loc, KEY_ENTITY));
            }
            
            // and add exit somewhere else
            Vector2 exitLoc = RandomSpawn(roomGenerator.rooms);
            objectSpawns.Add(new ObjectSpawn(exitLoc, EXIT_ENTITY));
        }

        
        /// <summary>
        /// decides if a container should be spawned here
        /// </summary>
        private void SpawnContainers(DungeonRoom room, MapInfo mapInfo, ref int numChests) {
            if(numChests >= mapInfo.maxLootChests) {
                return;
            }

            Vector2 spawnLoc = RandomSpawnInRoom(room);
            ObjectSpawn spawn = new ObjectSpawn(spawnLoc, "Chest");
            objectSpawns.Add(spawn);
            numChests += 1;
        }

        /// <summary>
        /// decides if any mobs should be spawned int the room
        /// </summary>
        private void SpawnMobs(DungeonRoom room, MapInfo mapInfo) {
            int numEnemies = rng.getInt(mapInfo.minEnemies, mapInfo.maxEnemies);
            for(int i = 0; i < numEnemies; i++) {
                Vector2 spawnLoc = RandomSpawnInRoom(room);

                string type = MapInfo.GetRandomFrom(mapInfo.enemySpawnTable, rng);
                ObjectSpawn spawn = new ObjectSpawn(spawnLoc, type);
                objectSpawns.Add(spawn);
            }
        }


        /// <summary>
        /// Digs out the input area by making its area into passabel tiles
        /// </summary>
        void DigArea(AreaMap.Tile[] tiles, int x1, int y1, int x2, int y2) {
            if(x2 < x1) {
                int tmp = x2;
                x2 = x1;
                x1 = tmp;
            }

            if(y2 < y1) {
                int tmp = y2;
                y2 = y1;
                y1 = tmp;
            }

            Terrain floor = GetTerrainType(Terrain.EMobilityLevel.Passable);

            for(int tilex = x1; tilex <= x2; tilex++) {
                for(int tiley = y1; tiley <= y2; tiley++) {
                    tiles[tilex + tiley * width].terrain = floor;
                }
            }
        }


        /// <summary>
        /// Generates a gabriel graph of the larger rooms in the room set
        /// </summary>
        private void CreateSparseRoomGraph(List<DungeonRoom> rooms) {

            List<DungeonRoom> unsearched = new List<DungeonRoom>(rooms.Count);
            Stack<DungeonRoom> toSearch = new Stack<DungeonRoom>();

            // construct the graph set first, obviously start with all of it unsearched
            for(int i = 0; i < rooms.Count; i++) {
                unsearched.Add(rooms[i]);
            }

            // start with one abritrary node, so just use the first one
            toSearch.Push(unsearched[0]);
            unsearched.RemoveAt(0);

            // Connect the graph with each node connected to its closes euclidian neighbor
            while(toSearch.Count > 0) {
                DungeonRoom currentRoom = toSearch.Pop();

                int bestIdx = -1;
                float bestDist = 99999.9f;
                for(int i = 0; i < unsearched.Count; i++) {
                    float dist = RoomDistSqr(unsearched[i], currentRoom);
                    if(dist < bestDist) {
                        bestIdx = i;
                        bestDist = dist;
                    }
                }

                if(bestIdx >= 0) {
                    // make it one directional connection so that only one hallway goes b/w each room
                    currentRoom.neighbors.Add(unsearched[bestIdx]);

                    toSearch.Push(unsearched[bestIdx]);
                    unsearched.RemoveAt(bestIdx);
                }
            }            
        }

        /// <summary>
        /// Digs a hallway b/w the rooms
        /// </summary>
        private void ConnectRooms(AreaMap.Tile[] tiles, Rect a, Rect b) {
            DigArea(tiles,
                a.x,
                a.y,
                b.x + b.w / 2,
                a.y);
            DigArea(tiles,
                b.x + b.w / 2,
                a.y,
                b.x + b.w / 2,
                b.y + b.h / 2);
        }


        /// <summary>
        /// Figures out where the doors should be for this room, adds an entry to the object spawn list
        /// </summary>
        private void GetDoors(AreaMap.Tile[] tiles, Rect room) {
            const int MAX_DOORS_PER_ROOM = 4;

            // iterate over the room, including the walls of it, looking for valid door locations
            int left = room.x - 1;
            int right = room.x + room.w + 1;
            int top = room.y - 1;
            int bottom = room.y + room.h + 1;

            // track number of doors added so that chance of more doors reduces after the first one
            int numDoors = 0;

            for(int x = left; x < right; x++) {
                for(int y = top; y < bottom; y++) {
                    // random chance of there being another door
                    float randVal = rng.getFloat(0.0f, 1.0f);
                    float deminishedReturns = randVal * (1.0f - numDoors / MAX_DOORS_PER_ROOM);
                    
                    if(deminishedReturns <= DOOR_RATE && ValidDoorLocation(tiles, x, y)) {
                        objectSpawns.Add(new ObjectSpawn(new Vector2(x, y), DOOR_ENTITY));
                        numDoors += 1;
                    }
                }
            }
        }


        /// <summary>
        ///  checks that there is only two walls on opposite sides of the tested location
        /// </summary>
        private bool ValidDoorLocation(AreaMap.Tile[] tiles, int x, int y) {
            // This check is performed by doing a kernal convolution on the tiles blocking state
            // these kernals will return 2 if there is only walls on the two sides, with nothing down the middle. They dont care about the corners
            int[,] horzKernal = {{ 0,-1, 0 },
                                 { 1,-1, 1 },
                                 { 0,-1, 0 }};

            int[,] vertKernal = {{ 0, 1, 0 },
                                 {-1,-1,-1 },
                                 { 0, 1 ,0 }};

            // door obv. must be in bounds
            if(x < 0 || y < 0 || x >= width || y >= height) {
                return false;
            }

            int horzConvolve = 0;
            int kx = 0, ky = 0;
            for(int i = x - 1; i <= x + 1; i++) {
                for(int j = y - 1; j <= y + 1; j++) {
                    bool isWall = IsWall(tiles, i, j);
                    horzConvolve += horzKernal[kx, ky] * (isWall ? 1 : 0);
                    ky++;
                }
                kx++;
                ky = 0;
            }
            
            // next do the vertical kernal
            int vertConvolve = 0;
            kx = 0; ky = 0;
            for(int i = x - 1; i <= x + 1; i++) {
                for(int j = y - 1; j <= y + 1; j++) {
                    bool isWall = IsWall(tiles, i, j);
                    vertConvolve += vertKernal[kx, ky] * (isWall ? 1 : 0);
                    ky++;
                }
                kx++;
                ky = 0;
            }
            
            // one of these kernals must show there are only two walls on opposite sides
            return (vertConvolve == 2 || horzConvolve == 2);
        }

        /// <summary>
        /// Returns true if the specified tile has a blocking type of terrain
        /// </summary>
        private bool IsWall(AreaMap.Tile[] tiles, int x, int y) {
            if(x < 0 || x >= width || y < 0 || y >= height) {
                return false;
            }
            return tiles[x + y * width].terrain.mobility == Terrain.EMobilityLevel.BlocksMove;
        }


        /// <summary>
        /// Gets teh square of the distance between the two rooms
        /// </summary>
        private float RoomDistSqr(DungeonRoom a, DungeonRoom b) {
            int dx = a.data.centerX() - b.data.centerX();
            int dy = a.data.centerY() - b.data.centerY();
            float distSqr = dx * dx + dy * dy;
            return distSqr;
        }


        /// <summary>
        /// Gets a random location in any dungeon rooms
        /// </summary>
        private Vector2 RandomSpawn(List<DungeonRoom> rooms) {
            int i = rng.getInt(0, rooms.Count - 1);
            return RandomSpawnInRoom(rooms[i]);
        }

        /// <summary>
        /// Returns a random location within the bounds of the input room
        /// </summary>
        private Vector2 RandomSpawnInRoom(DungeonRoom room) {
            Rect dimensions = room.data;
            return new Vector2(
                rng.getInt(dimensions.x, dimensions.x + dimensions.w - 1),
                rng.getInt(dimensions.y, dimensions.y + dimensions.h - 1)
                );
        }

    }
}
