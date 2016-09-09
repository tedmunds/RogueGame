using System;
using System.Collections.Generic;
using libtcod;
using RogueGame.Map;
using RogueGame.Data;

namespace RogueGame.Generation {

    public abstract class Generator {

        protected const string DOOR_ENTITY = "Door";
        protected const string ENTRANCE_ENTITY = "Entrance";
        protected const string EXIT_ENTITY = "Exit";
        protected const string KEY_ENTITY = "Item_Key";

        // Allows the generator to pass out a lit of objects it needs spawned to complete the map it has created
        public struct ObjectSpawn {
            public Vector2 location;
            public string entity;

            public ObjectSpawn(Vector2 location, string entity) {
                this.location = location;
                this.entity = entity;
            }
        }

        /// <summary>
        /// Object spawns is an optional list that will be filled out with objects spawn locations and entity names
        /// may be null
        /// </summary>
        public List<ObjectSpawn> objectSpawns;

        protected Terrain[] terrains;
        protected TCODRandom rng;
        protected MapInfo mapInfo;


        public Generator(TCODRandom rng, Terrain[] terrains, MapInfo mapInfo) {
            this.terrains = terrains;
            this.rng = rng;
            this.mapInfo = mapInfo;
        }

        /// <summary>
        /// Files in the tile array with tiles from this generators terrain set in a procedural way
        /// </summary>
        public abstract void GenerateMap(AreaMap.Tile[] tiles, int width, int height);

        /// <summary>
        /// Gives back a random terrain that has the input properties
        /// </summary>
        public Terrain GetTerrainType(Terrain.EMobilityLevel mobility) {
            if(terrains == null) {
                return null;
            }

            for(int i = 0; i < terrains.Length; i++) {
                if(terrains[i].mobility == mobility) {
                    return terrains[i];
                }
            }

            return null;
        }
    }
}
