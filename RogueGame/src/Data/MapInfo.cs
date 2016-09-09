using System;
using System.Xml.Serialization;
using libtcod;
using RogueGame.Map;

namespace RogueGame.Data {

   

    /// <summary>
    /// Defines a serializable class that holds all the data for a given type of map, like a dungeon or forest etc.
    /// includes the tileset 
    /// </summary>
    [XmlRoot("mapInfo")]
    public class MapInfo {

        /// <summary>
        /// Represents a thing that has some proportional chance of spawning
        /// </summary>
        public struct SpawnFrequency {
            [XmlAttribute("chance")]
            public float chance;

            [XmlAttribute("name")]
            public string name;
        }

        public enum EGeneratorType {
            Dungeon,
            Overworld,
        }

        [XmlElement("name")]
        public string mapInfoName;

        /// <summary>
        /// The type of IGenerator class to use to creat the map
        /// </summary>
        [XmlElement("genClass")]
        public EGeneratorType generatorType;

        [XmlElement("minEnemies")]
        public int minEnemies;
        [XmlElement("maxEnemies")]
        public int maxEnemies;

        [XmlElement("maxLootChests")]
        public int maxLootChests;

        [XmlElement("width")]
        public int width;
        [XmlElement("height")]
        public int height;

        /// <summary>
        /// Set of terrain tile types to use for this map
        /// </summary>
        [XmlArray("terrainTypes")]
        [XmlArrayItem("terrain")]
        public Terrain[] terrainSet;

        [XmlArray("spawnTable")]
        [XmlArrayItem("entry")]
        public SpawnFrequency[] enemySpawnTable;

        [XmlArray("lootTable")]
        [XmlArrayItem("entry")]
        public SpawnFrequency[] lootTable;


        // serializable class uses only empty constructor
        public MapInfo() { }

        // called by the object loader after it has been deserialized
        public static void PostLoadInitialization(MapInfo loaded) {
            for(int i = 0; i < loaded.terrainSet.Length; i++) {
                loaded.terrainSet[i].PostLoadInitialization();
            }

            // Normalize the enemy spawn table probablities and loot table. It's assumed no one wants to do this by hand in the data file.
            // instead the user just writes relative proportions as chances
            NormalizeProbabilities(ref loaded.enemySpawnTable);
            NormalizeProbabilities(ref loaded.lootTable);
        }


        /// <summary>
        /// Normalizes the chance values of each spawn frquency data element in the input list
        /// </summary>
        private static void NormalizeProbabilities(ref SpawnFrequency[] table) {
            float runningTotal = 0.0f;
            for(int i = 0; i < table.Length; i++) {
                runningTotal += table[i].chance;
            }

            float maxProb = runningTotal;

            for(int i = 0; i < table.Length; i++) {
                table[i].chance = table[i].chance / maxProb;
            }
        }


        /// <summary>
        /// Gets a name from the input spawn frequency table
        /// </summary>
        public static string GetRandomFrom(SpawnFrequency[] table, TCODRandom rng) {
            float randVal = rng.getFloat(0.0f, 1.0f);

            float totalProb = 0.0f;
            for(int i = 0; i < table.Length; i++) {
                totalProb += table[i].chance;

                if(randVal < totalProb) {
                    return table[i].name;
                }
            }

            return "";
        }


        /// <summary>
        /// Finds the terrain of given name in the loaded terrain set
        /// </summary>
        public Terrain GetTerrain(string name) {
            foreach(Terrain t in terrainSet) {
                if(t.name == name) {
                    return t;
                }
            }

            // otherwise just default to the 0
            return terrainSet[0];
        }









    }
}
