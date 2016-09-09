using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;
using RogueGame.Data;

namespace RogueGame.Generation {
    public class GenSimpleDungeon : Generator {
        
        // Generator params
        const int maxRooms = 20;
        const int minRooms = 10;
        const int minSize = 5;
        const int maxSize = 15;
        const int edgeBuffer = 1;

        private int width;
        private int height;


        public GenSimpleDungeon(TCODRandom rng, Terrain[] terrains, MapInfo mapInfo) 
            : base(rng, terrains, mapInfo) {
            
        }


        public override void GenerateMap(AreaMap.Tile[] tiles, int width, int height) {
            this.width = width;
            this.height = height;

            GenerateRooms(tiles);
        }



        private void GenerateRooms(AreaMap.Tile[] tiles) {
            int numRooms = rng.getInt(minRooms, maxRooms);

            int prevX = 0;
            int prevY = 0;
            for(int i = 0; i < numRooms; i++) {
                int xOrigin = rng.getInt(edgeBuffer, width - edgeBuffer);
                int yOrigin = rng.getInt(edgeBuffer, height - edgeBuffer);
                int w = rng.getInt(minSize, maxSize);
                int h = rng.getInt(minSize, maxSize);

                // for the first room, no hallways
                if(i == 0) {
                    prevX = xOrigin;
                    prevY = yOrigin;
                }

                FillRoom(tiles, xOrigin, yOrigin, w, h, prevX, prevY);
                prevX = xOrigin;
                prevY = yOrigin;
            }
        }


        private void FillRoom(AreaMap.Tile[] tiles, int x, int y, int w, int h, int lastx, int lasty) {
            // right and bottom
            int r = x + w;
            int b = y + h;

            if(x < 0) x = 0;
            if(r >= width) r = width - 1;
            if(y < 0) y = 0;
            if(b >= height) b = height - 1;

            Terrain floor = GetTerrainType(Terrain.EMobilityLevel.Passable);
            Terrain wall = GetTerrainType(Terrain.EMobilityLevel.BlocksMove);

            for(int i = x; i < r; i++) {
                for(int k = y; k < b; k++) {
                    // set tiles in this range to floors
                    tiles[i + k * width].terrain = floor;
                }
            }

            // Now make hallway to the last room
            for(int i = lastx; i != x; i += (lastx - x < 0) ? 1 : -1) {
                tiles[i + lasty * width].terrain = floor;
            }

            for(int k = lasty; k != y; k += (lasty - y < 0) ? 1 : -1) {
                tiles[x + k * width].terrain = floor;
            }
        }



    }
}
