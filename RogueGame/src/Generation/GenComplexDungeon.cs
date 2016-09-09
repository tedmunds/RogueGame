using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;
using RogueGame.Data;

namespace RogueGame.Generation {
    public class GenComplexDungeon : Generator {

        private struct DungeonRoom {
            public int x;
            public int y;
            public int w;
            public int h;

            // for room graph
            public List<DungeonRoom> neighbors;

            public DungeonRoom(int x, int y, int w, int h) {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
                neighbors = new List<DungeonRoom>(1);
            }

            public int Area() { return w * h; }
            public int centerX() { return x + w / 2; }
            public int centerY() { return y + h / 2; }
        }


        private const int minRoomSize = 5;
        private const int maxRoomSize = 15;
        private const int bigRoomThreshold = 10;

        private const int baseRoomNum = 50;
        private const int startRoomRadius = 20;
        private const int roomSteeringRadius = 20;

        private int width;
        private int height;

        public GenComplexDungeon(TCODRandom rng, Terrain[] terrains, MapInfo mapInfo) 
            : base(rng, terrains, mapInfo) {
        }



        public override void GenerateMap(AreaMap.Tile[] tiles, int width, int height) {
            this.width = width;
            this.height = height;

            DungeonRoom[] rooms = GenerateRooms(tiles, baseRoomNum);
            SteerRoomsArapt(rooms);
            List<DungeonRoom> roomGraph = CreateSparseRoomGraph(rooms);

            List<Vector2> doors;
            AddHallways(tiles, rooms, roomGraph, out doors);

            // add a door for each location
            objectSpawns = new List<ObjectSpawn>();
            foreach(Vector2 doorLoc in doors) {
                // must re-check if the door is still valid since mor digging may have occured
                if(ValidDoorLocation(tiles, doorLoc.X, doorLoc.Y)) {
                    objectSpawns.Add(new ObjectSpawn(doorLoc, "Door"));
                }
            }
        }



        private DungeonRoom[] GenerateRooms(AreaMap.Tile[] tiles, int numRooms) {
            DungeonRoom[] roomSet = new DungeonRoom[numRooms];

            // Generate the rooms clustered in the center
            for(int i = 0; i < numRooms; i++) {
                int x = width / 2 + rng.getGaussianRangeInt(-startRoomRadius, startRoomRadius);
                int y = height / 2 + rng.getGaussianRangeInt(-startRoomRadius, startRoomRadius);
                int w = WeightedRandom(minRoomSize, maxRoomSize);
                int h = WeightedRandom(minRoomSize, maxRoomSize);

                DungeonRoom room = new DungeonRoom(x - w / 2, y - h / 2, w, h);
                roomSet[i] = room;
            }

            return roomSet;
        }



        private void SteerRoomsArapt(DungeonRoom[] rooms) {
            const int maxIterations = 10;

            // Move the rooms apart from each other until they are no longer touching
            bool anyIntersections = true;
            int iteration = 0;

            while(anyIntersections) {
                bool didAnyMove = false;

                for(int i = 0; i < rooms.Length; i++) {
                    bool moved = MoveRoomAwayFromNeighbors(rooms, i);
                    if(moved) {
                        didAnyMove = true;
                    }
                }

                anyIntersections = didAnyMove;
                iteration += 1;
                if(iteration > maxIterations) {
                    break;
                }
            }
        }




        /// <summary>
        /// Moves the room away from all of its neighbors, returns whther or not it was moved
        /// </summary>
        private bool MoveRoomAwayFromNeighbors(DungeonRoom[] rooms, int thisRoom) {
            const float maxDistScale = 10.0f;
            int numRoomsInRadius = 0;

            for(int i = 0; i < rooms.Length; i++) {
                if(i != thisRoom && RoomsOverlap(rooms[i], rooms[thisRoom])) {
                    Vector2 displacement = new Vector2(rooms[thisRoom].centerX() - rooms[i].centerX(),
                                                       rooms[thisRoom].centerY() - rooms[i].centerY());

                    float moveMagnitude = displacement.Magnitude() / maxDistScale;
                    if(moveMagnitude < 1.0f) {
                        rooms[thisRoom].x += (int)(displacement.x * moveMagnitude);
                        rooms[thisRoom].y += (int)(displacement.y * moveMagnitude);

                        CheckRoomInBounds(ref rooms[thisRoom]);

                        numRoomsInRadius += 1;
                    }
                }
            }

            return (numRoomsInRadius > 0);
        }


        /// <summary>
        /// Generates a gabriel graph of the larger rooms in the room set
        /// </summary>
        private List<DungeonRoom> CreateSparseRoomGraph(DungeonRoom[] rooms) {
            // List of rooms to make the graph
            List<DungeonRoom> bigRooms = new List<DungeonRoom>(baseRoomNum);

            List<DungeonRoom> unsearched = new List<DungeonRoom>(baseRoomNum);
            Stack<DungeonRoom> toSearch = new Stack<DungeonRoom>();

            // construct the graph set first
            for(int i = 0; i < rooms.Length; i++) {
                if(rooms[i].Area() >= (bigRoomThreshold * bigRoomThreshold)) {
                    bigRooms.Add(rooms[i]);
                    unsearched.Add(rooms[i]);
                }
            }
            
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
                    currentRoom.neighbors.Add(unsearched[bestIdx]);
                    //unsearched[bestIdx].neighbors.Add(currentRoom);
                    toSearch.Push(unsearched[bestIdx]);
                    unsearched.RemoveAt(bestIdx);
                }

            }

            return bigRooms;
        }



        private void AddHallways(AreaMap.Tile[] tiles, DungeonRoom[] allRooms, List<DungeonRoom> mainRoomGraph, out List<Vector2> doorLocations) {
            doorLocations = new List<Vector2>();

            for(int i = 0; i < mainRoomGraph.Count; i++) {
                DungeonRoom mainRoom = mainRoomGraph[i];

                DigOutRoom(tiles, mainRoom);

                // and check all of this rooms connections to create hallways
                for(int j = 0; j < mainRoom.neighbors.Count; j++) {
                    DungeonRoom neighborRoom = mainRoom.neighbors[j];

                    // add a doorway for this hallway
                    //doorLocations.Add(DoorLocation(mainRoom, neighborRoom));

                    DigOutHallway(tiles, mainRoom, neighborRoom, allRooms, doorLocations);
                }
            }
        }

        // Figure out where the door should be based on the room relations
        private Vector2 DoorLocation(DungeonRoom mainRoom, DungeonRoom targetRoom) {
            Vector2 loc = new Vector2(mainRoom.centerX(), mainRoom.centerY());

            float deltaX = Math.Abs(loc.x - targetRoom.centerX());
            float deltaY = Math.Abs(loc.y - targetRoom.centerY());

            if(deltaX >= deltaY) {
                int direction = -Math.Sign(loc.x - targetRoom.centerX());
                loc.x += direction * (mainRoom.w / 2);
            }
            else {
                int direction = Math.Sign(loc.y - targetRoom.centerY());
                loc.y += direction * (mainRoom.h / 2);
            }

            return loc;
        }


        private float RoomDistSqr(DungeonRoom a, DungeonRoom b) {
            int dx = a.centerX() - b.centerX();
            int dy = a.centerY() - b.centerY();
            float distSqr = dx * dy + dy * dy;
            return distSqr;
        }


        /// <summary>
        /// Checks if the two rooms intersect at all
        /// </summary>
        private bool RoomsOverlap(DungeonRoom a, DungeonRoom b) {
            if(a.x - 1 + a.w + 2 < b.x - 1 || b.x - 1 + b.w + 2 < a.x - 1 ||
               a.y - 1 + a.h + 2 < b.h - 1 || b.y - 1 + b.h + 2 < a.y - 1) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the point is inside the room
        /// </summary>
        private bool PointInRoom(int x, int y, DungeonRoom room) {
            return (x >= room.x && x < room.x + room.w &&
                   y >= room.y && y < room.y + room.h);
        }


        /// <summary>
        /// Checks that the input room is inside tile area, and moves it in if its not
        /// </summary>
        private void CheckRoomInBounds(ref DungeonRoom room) {
            if(room.x < 0) {
                room.x = 0;
            }

            if(room.x + room.w >= width) {
                room.x = width - room.w;
            }

            if(room.y < 0) {
                room.y = 0;
            }

            if(room.y + room.h >= height) {
                room.y = height - room.h;
            }
        }



        private void DigOutRoom(AreaMap.Tile[] tiles, DungeonRoom room) {
            int r = room.x + room.w;
            int b = room.y + room.h;

            if(room.x < 0) room.x = 0;
            if(r >= width) r = width - 1;
            if(room.y < 0) room.y = 0;
            if(b >= height) b = height - 1;


            Terrain floor = GetTerrainType(Terrain.EMobilityLevel.Passable);
            Terrain wall = GetTerrainType(Terrain.EMobilityLevel.BlocksMove);

            for(int i = room.x; i < r; i++) {
                for(int k = room.y; k < b; k++) {
                    // set tiles in this range to floors
                    tiles[i + k * width].terrain = floor;
                }
            }
        }

        // Digs from a to b, and any other non-graph rooms while digging, it also digs out that room
        private void DigOutHallway(AreaMap.Tile[] tiles, DungeonRoom a, DungeonRoom b, 
            DungeonRoom[] otherRooms, List<Vector2> doorLocations) {

            Terrain floor = GetTerrainType(Terrain.EMobilityLevel.Passable);
            bool placedDoor = false;

            for(int x = a.centerX(); x != b.centerX(); x += (a.centerX() < b.centerX()) ? 1 : -1) {
                tiles[x + a.centerY() * width].terrain = floor;

                // check if the current tile is in another room
                for(int i = 0; i < otherRooms.Length; i++) {
                    // if its not one of the rooms that is already being connected
                    if(otherRooms[i].x != a.x && otherRooms[i].y != a.y && otherRooms[i].x != b.x && otherRooms[i].y != b.y) {

                        // check if this hallway is inside the room, and dig out that room if it is
                        if(PointInRoom(x, a.centerY(), otherRooms[i])) {
                            DigOutRoom(tiles, otherRooms[i]);
                            break;
                        }

                        if(!placedDoor && ValidDoorLocation(tiles, x, a.centerY())) {
                            doorLocations.Add(new Vector2(x, a.centerY()));
                            placedDoor = true;
                        }
                    }
                }
            }

            placedDoor = false;

            for(int y = a.centerY(); y != b.centerY(); y += (a.centerY() < b.centerY()) ? 1 : -1) {
                tiles[b.centerX() + y * width].terrain = floor;

                for(int i = 0; i < otherRooms.Length; i++) {
                    // if its not one of the rooms that is already being connected
                    if(otherRooms[i].x != a.x && otherRooms[i].y != a.y && otherRooms[i].x != b.x && otherRooms[i].y != b.y) {

                        // check if this hallway is inside the room, and dig out that room if it is
                        if(PointInRoom(b.centerX(), y, otherRooms[i])) {
                            DigOutRoom(tiles, otherRooms[i]);
                            
                            break;
                        }

                        if(!placedDoor && ValidDoorLocation(tiles, b.centerX(), y)) {
                            doorLocations.Add(new Vector2(b.centerX(), y));
                            placedDoor = true;
                        }
                    }
                }
            }
        }


        // checks that there is only two walls on opposite sides of the tested location
        private bool ValidDoorLocation(AreaMap.Tile[] tiles, int x, int y) {
            int[,] horzKernal = {{0,0,0},
                                 {1,0,1},
                                 {0,0,0} };

            int[,] vertKernal = {{0,1,0},
                                 {0,0,0},
                                 {0,1,0} };

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

            if(horzConvolve == 2) {
                return true;
            }

            // otherwise try the vertical kernal
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

            if(vertConvolve == 2) {
                return true;
            }

            return false;
        }


        private bool IsWall(AreaMap.Tile[] tiles, int x, int y) {
            if(x < 0 || x >= width || y < 0 || y >= height) {
                return false;
            }
            return tiles[x + y * width].terrain.mobility == Terrain.EMobilityLevel.BlocksMove;
        }


        /// <summary>
        /// Random number b/w 0 and max, that is weighted towards values closer to zero
        /// </summary>
        public int WeightedRandom(int min, int max) {
            const float weightExponent = 2.0f;
            float randWeight = (float)Math.Pow(rng.getFloat(0.0f, 1.0f), weightExponent);

            return min + (int)(randWeight * (max - min));
        }


    }
}
