using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using libtcod;

using RogueGame.Map;
using RogueGame.Generation;
using RogueGame.Data;
using RogueGame.Components;

namespace RogueGame {
    
    [Serializable()]
    public class World : ISerializable {

        [NonSerialized]
        private Engine engine;

        [NonSerialized]
        private TCODRandom rng;

        [NonSerialized]
        public MapInfo currentMapInfo;

        /// <summary>
        /// Main map of tiles that is currently active
        /// </summary>
        public AreaMap currentMap;

        /// <summary>
        /// Cached reference to the player entity
        /// </summary>
        public Entity player;

        /// <summary>
        /// List of other maps that the player has visited and are still cached
        /// </summary>
        public List<AreaMap> mapCache;
        public int mapIdx;
        
        public World() {
            engine = Engine.instance;
            rng = TCODRandom.getInstance();         
        }

        
        /// <summary>
        /// Cleans up the current level and regenerates a new one
        /// </summary>
        public void InitializeNewLevel() {
            // TODO: much more generic way of setting up the map data
            currentMapInfo = XMLObjectLoader.LoadXMLObject<MapInfo>("data\\xml\\DefaultMapInfo.xml", MapInfo.PostLoadInitialization);

            if(currentMap != null) {
                if(mapCache == null) {
                    mapCache = new List<AreaMap>();
                }

                mapCache.Add(currentMap);
                mapIdx += 1;

                CleanUpMap(currentMap);
                currentMap = null;
            }
            
            // Choose which generator to use
            Generator mapGenerator = null;
            switch(currentMapInfo.generatorType) {
                case MapInfo.EGeneratorType.Dungeon:
                    if(rng.getFloat(0.0f, 1.0f) > 0.5f) {
                        mapGenerator = new GenCaveDungeon(rng, currentMapInfo.terrainSet, currentMapInfo);
                    }
                    else {
                        mapGenerator = new GenRoomDungeon(rng, currentMapInfo.terrainSet, currentMapInfo);
                    }
                    break;
                case MapInfo.EGeneratorType.Overworld:
                    // TODO: new generator class
                    break;
                default:
                    mapGenerator = new GenSimpleDungeon(rng, currentMapInfo.terrainSet, currentMapInfo);
                    break;
            }
            
            // Create the area map from the selected generator
            currentMap = new AreaMap(this, currentMapInfo.width, currentMapInfo.height, mapGenerator);
            
            // spawn the entities requested from the generation algorithm, stuff like doors and other static objects
            if(mapGenerator.objectSpawns != null) {
                foreach(var spawn in mapGenerator.objectSpawns) {

                    // special case handles player spawn, need to cache the reference
                    if(spawn.entity == "Player") {
                        SpawnPlayer(spawn.location);
                        continue;
                    }

                    Entity spawned = SpawnEntity(spawn.entity, spawn.location);

                    // handle a few other special cases
                    if(spawn.entity == "Chest") {
                        PopulateLootChest(spawned);
                    }
                    if(spawn.entity == "Entrance") {
                        currentMap.entranceLoc = spawn.location;
                    }
                    if(spawn.entity == "Exit") {
                        currentMap.exitLoc = spawn.location;
                    }
                }
            }

            // give the map a first LOS update
            currentMap.UpdateLOSFrom(player.position, 10);
        }

        /// <summary>
        /// Cleans up the input map, in particular removing registered entites from any caches they are in
        /// </summary>
        public void CleanUpMap(AreaMap map) {
            DespawnEntity(player);

            foreach(var e in map.levelEntities) {
                engine.UnregisterEntity(e);
            }
        }


        /// <summary>
        /// Travels to a level that is in the map cache. If the input index does not exist nothing will happen
        /// </summary>
        public void GotoExistingLevel(int idx, bool spawnAtEntrance = false) {
            if(idx < 0 || idx >= mapCache.Count) {
                return;
            }

            // cache the current map so that it will be used when the player returns
            if(currentMap != null && !mapCache.Contains(currentMap)) {
                mapCache.Add(currentMap);
            }
            
            if(currentMap != null) {
                CleanUpMap(currentMap);
            }
            
            currentMap = mapCache[idx];
            mapIdx = idx;

            // re-register the entities. player can be spawned at either entrance or exit
            Vector2 playerSpawn = spawnAtEntrance ? currentMap.entranceLoc : currentMap.exitLoc;
            SpawnPlayer(playerSpawn);
            foreach(var e in currentMap.levelEntities) {
                engine.RegisterEntity(e);
            }
        }

        /// <summary>
        /// Goes to a deeper level, if there is a cached map that is deeper than the current one it will use that
        /// otherwise it will generate a new map
        /// </summary>
        public void GotoDeeperLevel() {
            if(mapCache != null && mapIdx < mapCache.Count - 1) {
                GotoExistingLevel(mapIdx + 1, true);
            }
            else {
                InitializeNewLevel();
            }
        }

        /// <summary>
        /// Makes a new map and moves the player to it. The type of level is dependant on the input transition mode
        /// options are:
        /// PREVIOUS, DUNGEON_NEXT DUNGEON_ENTER DUNGEON_EXIT
        /// </summary>
        public void GotoNewLevel(string transitionMode) {
            var getPlayerName = (EGetScreenName)player.FireEvent(new EGetScreenName());

            if(transitionMode == "PREVIOUS") {
                GotoExistingLevel(mapIdx - 1);
                Engine.LogMessage("%2" + getPlayerName.text + "% walks back up the staircase");
            }
            else if(transitionMode == "DUNGEON_NEXT") {
                // TODO: load new level of dungeon, save current one to return to
                GotoDeeperLevel();
                Engine.LogMessage("%2" + getPlayerName.text + "% travels deeper");
            }
            else if(transitionMode == "DUNGEON_ENTER") {
                // TODO: generate a new dungeon, cache the overworld
            }
            else if(transitionMode == "DUNGEON_EXIT") {
                // TODO: exit dungeon saving it, return to the overworld
            }
            else {
                // do nothing in case of malformed code
                Console.WriteLine("World::GotoNewLevel - Could not handle transition mode [" + transitionMode + "]... Possibly check potal blueprint for this string.");
            }
            
            Engine.LogMessage("%2" + getPlayerName.text + "% has arrived in %2" + currentMapInfo.mapInfoName + "%");
        }




        private void SpawnPlayer(Vector2 location) {
            if(player == null) {
                player = SpawnEntity("Player", location);
            }
            else {
                SpawnExisting(player, location);
            }
        }


        /// <summary>
        /// Spawns the entity instance from the given prototype on the current map
        /// </summary>
        public Entity SpawnEntity(string prototype, Vector2 position) {
            Entity e = engine.data.InstantiateEntity(prototype);
            engine.RegisterEntity(e);

            e.position = position;
            currentMap.levelEntities.Add(e);
            currentMap.PutOnTile(e, position.X, position.Y);
            return e;
        }

        /// <summary>
        /// Places the already intialized entity in the world and registers it etc. 
        /// Assumes the entity is just initialized and not in the level list or anything
        /// </summary>
        public void SpawnExisting(Entity e, Vector2 position) {
            engine.RegisterEntity(e);
            e.position = position;
            currentMap.levelEntities.Add(e);
            currentMap.PutOnTile(e, position.X, position.Y);
        }

        /// <summary>
        /// Removes the entity from the level list, de-registers it and removes it from the map
        /// </summary>
        public void DespawnEntity(Entity e) {
            engine.UnregisterEntity(e);            
            currentMap.RemoveFromTile(e);
            currentMap.levelEntities.Remove(e);
        }
        
        private void PopulateLootChest(Entity chest) {
            if(currentMapInfo == null) {
                return;
            }
            
            // load the chest with one item for now. TODO: loot table 
            var inv = chest.Get<InvComponent>();
            if(inv == null) {
                return;
            }

            inv.items.Add(engine.data.InstantiateEntity(RandomItem()));            
        }

        /// <summary>
        /// returns a valid spawn location
        /// </summary>
        private Vector2 RandomSpawnLocation() {
            int x = rng.getInt(0, currentMap.width - 1);
            int y = rng.getInt(0, currentMap.height - 1);
            Vector2 spawnLoc = new Vector2(x, y);
            return currentMap.GetNearestValidMove(spawnLoc);
        }


        /// <summary>
        /// Gets a random enemy from the current maps spawn table
        /// </summary>
        /// <returns></returns>
        public string RandomEnemy() {
            return MapInfo.GetRandomFrom(currentMapInfo.enemySpawnTable, rng);
        }

        /// <summary>
        /// Gets a random item from the maps loot table
        /// </summary>
        /// <returns></returns>
        public string RandomItem() {
            return MapInfo.GetRandomFrom(currentMapInfo.lootTable, rng);
        }


        /// <summary>
        /// Cycles through the entities so they can take turns
        /// </summary>
        public void ProcessTurns() {
            const int MIN_INITIATIVE = 2000;

            // always start with the player, b/c last turn cycle ended with the player
            player.FireEvent(new ENewTurn());

            var turnQueue = new Utilities.MinHeap<TurnComponent>();

            foreach(Entity e in currentMap.levelEntities) {
                if(e == player) {
                    continue;
                }

                // check if this turn taker can be added to the queue yet, or if it needs to wait longer
                TurnComponent turnTaker = e.Get<TurnComponent>();
                if(turnTaker != null) {
                    turnTaker.initiative += turnTaker.speed;

                    if(turnTaker.initiative >= MIN_INITIATIVE) {
                        turnQueue.Add(turnTaker);
                        turnTaker.initiative = 0;
                    }
                }
            }
            
            // now actually perform the turn cycle in order of most initiative
            while(turnQueue.Count > 0) {
                Entity e = turnQueue.ExtractDominating().owner;
                e.FireEvent(new ENewTurn());
            }
        }



        // =================================================================================================================================================================================================================
        // Serialization handling: 
        // =================================================================================================================================================================================================================
        
        // Implement this method to serialize data. The method is called on serialization.
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            if(mapCache == null) {
                mapCache = new List<AreaMap>();
                mapCache.Add(currentMap);
            }
            
            info.AddValue("mapCache", mapCache, typeof(List<AreaMap>));
            info.AddValue("mapIdx", mapIdx, typeof(int));
        }



        // The special constructor is used to deserialize values.
        public World(SerializationInfo info, StreamingContext context) {
            // TODO: move this to more generic place
            currentMapInfo = XMLObjectLoader.LoadXMLObject<MapInfo>("data\\xml\\DefaultMapInfo.xml", MapInfo.PostLoadInitialization);

            engine = Engine.instance;
            rng = TCODRandom.getInstance();
            player = null;

            mapCache = (List<AreaMap>)info.GetValue("mapCache", typeof(List<AreaMap>));
            mapIdx = (int)info.GetValue("mapIdx", typeof(int));
        }


        // Immediately before a world state is about to be deserialzed. The existing worldstate needs to cleanup loose ends
        public void PreDeserializeCleanup() {
            foreach(var e in currentMap.levelEntities) {
                engine.UnregisterEntity(e);
            }
        }

        // After a world state has been deserialzed
        public void PostDeserializeInit() {
            mapCache[mapIdx].PostDeserializeInit();
            currentMap = mapCache[mapIdx];

            // re-register the entities. player can be spawned at either entrance or exit            
            foreach(var e in currentMap.levelEntities) {
                engine.RegisterEntity(e);

                if(e.name == "Player") {
                    player = e;
                }
            }
        }







    }
}
