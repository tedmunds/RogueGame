using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;
using RogueGame.Data;
using RogueGame.Utilities;

namespace RogueGame.Generation {
    /// <summary>
    /// Generates a more organic natural cave structure with more large open spaces
    /// </summary>
    public class GenCaveDungeon : Generator {
        private const int MIN_CRAWLERS = 8;
        private const int MAX_CRAWLERS = 8;

        private const int NUM_ITERATIONS = 100;
        private const int START_AREA_SIZE = 40;

        private const float NOISE_ROUGHNESS = 0.5f;
        private const float NOISE_FREQUENCY = 10.0f;
        private const float MAX_RADIUS = 3.0f;
        private const float MIN_RADIUS = 1.0f;

        private delegate void DigHandler(int x, int y);

        // Crawler moves around the map to dig out caves
        private struct Crawler {
            public int x;
            public int y;
            public float radius;
            public Vector2 direction;
            public bool isDead;

            public Crawler(Vector2 direction) {
                x = 0;
                y = 0;
                radius = 0;
                isDead = false;
                this.direction = direction;
            }
        }

        // perlin noise object used to steer crawlers
        private PerlinNoise perlin;

        private int width;
        private int height;
        
        public GenCaveDungeon(TCODRandom rng, Terrain[] terrains, MapInfo mapInfo) 
            : base(rng, terrains, mapInfo) {
            perlin = new PerlinNoise(rng.getInt(0, 2147483647));
        }


        public override void GenerateMap(AreaMap.Tile[] tiles, int width, int height) {
            this.width = width;
            this.height = height;
            int startx = width / 2;
            int starty = height / 2;

            // list of tiles that were added to the cave, for choosing valid spawn locations
            Dictionary<Vector2, Vector2> caveTiles = new Dictionary<Vector2, Vector2>();

            // Create a set of crawlers that will be guided around the space by the perlin noise, carving out an area of the map while they are at it
            int numCrawlers = rng.getInt(MIN_CRAWLERS, MAX_CRAWLERS);

            // intialize the crawler directions
            Crawler[] crawlers = new Crawler[numCrawlers];
            for(int i = 0; i < numCrawlers; i++) {
                int dirx = rng.getInt(-1, 1);
                int diry = rng.getInt(-1, 1);
                if(dirx == 0 && diry == 0) {
                    dirx = 1;
                }

                crawlers[i] = new Crawler(new Vector2(dirx, diry));
                crawlers[i].x = rng.getInt(width / 2 - START_AREA_SIZE, width / 2 + START_AREA_SIZE);
                crawlers[i].y = rng.getInt(height / 2 - START_AREA_SIZE, height / 2 + START_AREA_SIZE);
            }
            

            // perform the actual dungeon digging. Update each crawler a set number of times
            for(int i = 0; i < NUM_ITERATIONS; i++) {
                for(int cIdx = 0; cIdx < numCrawlers; cIdx++) {
                    UpdateCrawler(ref crawlers[cIdx]);

                    // dig out the crawlers new area
                    DigArea(tiles, crawlers[cIdx], caveTiles);
                }
            }

            // and send crawlers back to some central location in order to ensure a connected level
            Vector2 homeLoc = new Vector2(
                rng.getInt(width / 2 - START_AREA_SIZE, width / 2 + START_AREA_SIZE),
                rng.getInt(height / 2 - START_AREA_SIZE, height / 2 + START_AREA_SIZE)
                );

            Crawler prevCrawler = crawlers[0];
            for(int cIdx = 1; cIdx < numCrawlers; cIdx++) {
                ReturnCrawlerToHome(tiles, ref crawlers[cIdx], new Vector2(prevCrawler.x, prevCrawler.y), caveTiles);
            }

            // spawn some items, including palyer and entrance / exit
            List<Vector2> validSpawns = new List<Vector2>(caveTiles.Values);
            Vector2 playerSpawn = validSpawns[rng.getInt(0, validSpawns.Count - 1)];

            objectSpawns = new List<ObjectSpawn>();
            objectSpawns.Add(new ObjectSpawn(playerSpawn, "Player"));
            objectSpawns.Add(new ObjectSpawn(playerSpawn, ENTRANCE_ENTITY));

            Vector2 exitSpawn = validSpawns[rng.getInt(0, validSpawns.Count - 1)];
            objectSpawns.Add(new ObjectSpawn(exitSpawn, EXIT_ENTITY));

            // spawn mobs
            for(int i = 0; i < mapInfo.maxEnemies; i++) {
                Vector2 spawnLoc = validSpawns[rng.getInt(0, validSpawns.Count - 1)];

                string type = MapInfo.GetRandomFrom(mapInfo.enemySpawnTable, rng);
                ObjectSpawn spawn = new ObjectSpawn(spawnLoc, type);
                objectSpawns.Add(spawn);
            }

            // spawn chest
            Vector2 chestLoc = validSpawns[rng.getInt(0, validSpawns.Count - 1)];
            Vector2 keyLoc = validSpawns[rng.getInt(0, validSpawns.Count - 1)];
            
            objectSpawns.Add(new ObjectSpawn(chestLoc, "Chest"));
            objectSpawns.Add(new ObjectSpawn(keyLoc, KEY_ENTITY));
        }

        

        private void UpdateCrawler(ref Crawler crawler) {
            Vector2[] dirOffsets = new Vector2[] {
                new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
                new Vector2(-1, 0),                      new Vector2(1, 0),
                new Vector2(-1, 1),  new Vector2(0, 1),  new Vector2(1, 1)
            };

            if(crawler.isDead) {
                return;
            }

            // decide what direction to move in by sampling the perlin around the location and previous direction
            Vector2 sampleLoc = new Vector2(crawler.x, crawler.y);
            Vector2 direction = GetCrawlerDirection(crawler);

            // move to the new location
            sampleLoc += direction;
            crawler.x = sampleLoc.X;
            crawler.y = sampleLoc.Y;
            crawler.direction = direction;

            // decide the new radius by looking at current radius and the perlin amplitude
            float cellAmplitude = perlin.GetPerlinNoise(crawler.x, crawler.y, NOISE_ROUGHNESS);
            crawler.radius += cellAmplitude;
            if(crawler.radius > MAX_RADIUS) {
                crawler.radius = MAX_RADIUS;
            }
            if(crawler.radius < MIN_RADIUS) {
                crawler.radius = MIN_RADIUS;
            }
        }



        private void ReturnCrawlerToHome(AreaMap.Tile[] tiles, ref Crawler crawler, Vector2 homeLoc, Dictionary<Vector2, Vector2> caveTiles) {
            // move towards the home location, with some jiggling thrown in to make it more organic looking
            while(crawler.x != homeLoc.X || crawler.y != homeLoc.Y) {
                int dx = Math.Sign(homeLoc.X - crawler.x);
                int dy = Math.Sign(homeLoc.Y - crawler.y);

                crawler.x += dx;
                crawler.y += dy;

                crawler.radius += rng.getGaussianRangeFloat(-5, 5);
                if(crawler.radius > MAX_RADIUS) {
                    crawler.radius = MAX_RADIUS;
                }
                if(crawler.radius < MIN_RADIUS) {
                    crawler.radius = MIN_RADIUS;
                }

                DigArea(tiles, crawler, caveTiles);
            }
        }



        private Vector2 GetCrawlerDirection(Crawler crawler) {
            Vector2[] dirOffsets = new Vector2[] {
                new Vector2(-1, -1), new Vector2(0, -1), new Vector2(1, -1),
                new Vector2(-1, 0),                      new Vector2(1, 0),
                new Vector2(-1, 1),  new Vector2(0, 1),  new Vector2(1, 1)
            };

            Vector2 dir = new Vector2(0, 0);
            Vector2 location = new Vector2(crawler.x, crawler.y);

            int highestDir = 0;
            float highestSample = -9999.9f;
            for(int i = 0; i < dirOffsets.Length; i++) {
                Vector2 sampleLoc = location + dirOffsets[i];

                float sample = perlin.Sample(location.x * NOISE_FREQUENCY, sampleLoc.y * NOISE_FREQUENCY);
                sample = Math.Abs(sample);

                // weight against moving backwards, rebalance the dot product to be from 0 to 1
                float momentum = Vector2.Dot(dirOffsets[i].Normalized(), crawler.direction.Normalized());
                momentum = (1.0f + momentum) / 2.0f;

                sample = sample * momentum;

                // also try to move away from the edge based on the square of the distance to the edge
                Vector2 nearestEdgeNormal;
                float edgeDist = GetNearestEdgeNormal(sampleLoc, out nearestEdgeNormal);
                float edgeWeight = 0.0f;
                if(nearestEdgeNormal.X > nearestEdgeNormal.Y) { // use width
                    edgeWeight = (edgeDist / (width / 2.0f - 1));
                }
                else { // use height
                    edgeWeight = (edgeDist / (height / 2.0f - 1));
                }

                // the closer to the edge it is, the stronger the wieght
                edgeWeight = edgeWeight * edgeWeight * edgeWeight;
                edgeWeight = 1.0f - edgeWeight;

                // reduce the sample by how close to the edge it is
                sample = sample * (1.0f - edgeWeight);

                if(sample > highestSample) {
                    highestSample = sample;
                    highestDir = i;
                }
            }

            dir = dirOffsets[highestDir];
            return dir;
        }



        private void DigArea(AreaMap.Tile[] tiles, Crawler crawler, Dictionary<Vector2, Vector2> caveTiles) {
            // dig out the crawlers defined space.
            int r = (int)crawler.radius;
            
            DigRect(tiles, crawler.x - r, crawler.y - r,
                crawler.x + r, crawler.y + r,
                (x, y) => {
                    var tile = new Vector2(x, y);
                    if(!caveTiles.ContainsKey(tile)) {
                        caveTiles.Add(tile, tile);
                    }
                });
        }

     


        /// <summary>
        /// Digs out the input area by making its area into passabel tiles
        /// </summary>
        void DigRect(AreaMap.Tile[] tiles, int x1, int y1, int x2, int y2, DigHandler onDig = null) {
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
                    if(tilex < 1 || tilex >= width - 1 || tiley < 1 || tiley >= height - 1) {
                        continue;
                    }

                    tiles[tilex + tiley * width].terrain = floor;
                    if(onDig != null) {
                        onDig(tilex, tiley);
                    }
                }
            }
        }

        /// <summary>
        /// gets the normal of the nearest edge, as well as returns the distance to that edge
        /// </summary>
        private float GetNearestEdgeNormal(Vector2 loc, out Vector2 normal) {
            float bestDist = 9999999;
            normal = new Vector2(0, 0);
            if(loc.x < width / 2) {
                bestDist = loc.x - 1;
                normal = new Vector2(1, 0);
            }
            else {
                bestDist = width - 1 - loc.x;
                normal = new Vector2(-1, 0);
            }

            if(loc.y < height / 2) {
                if(loc.y < bestDist) {
                    bestDist = loc.y - 1;
                    normal = new Vector2(0, 1);
                }
            }
            else {
                if(height - loc.y < bestDist) {
                    bestDist = height - 1 - loc.y;
                    normal = new Vector2(0, -1);
                }
            }

            return bestDist;
        }

    }
}
