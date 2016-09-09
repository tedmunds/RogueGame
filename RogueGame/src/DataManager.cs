using System.Collections.Generic;
using RogueGame.Components;
using RogueGame.Data;

namespace RogueGame {

    /// <summary>
    /// Data manager handles the organization of data Otehr things that wantto create instances of entites go through here
    /// </summary>
    public class DataManager {

        private Dictionary<string, Entity> prototypes;

        // the next id that will be assigned to the next entity that is instantiated
        private int nextId = 0;

        /// <summary>
        /// Load all game content on engine creation
        /// </summary>
        public void LoadContent() {
            prototypes = new Dictionary<string, Entity>();
            LoadPrototypes("TestBlueprints.xml");
            LoadPrototypes("WeaponBlueprints.xml");
            LoadPrototypes("SkillBlueprints.xml");
        }


        private void LoadPrototypes(string file) {
            Entity[] blueprints = BlueprintLoader.LoadBlueprintSet(file);
            foreach(Entity e in blueprints) {
                prototypes.Add(e.name, e);
            }
        }


        /// <summary>
        /// Creates a new instance of the entity based on the input prototype
        /// </summary>
        public Entity InstantiateEntity(string prototypeName) {
            Entity proto;
            if(prototypes.TryGetValue(prototypeName, out proto)) {
                Entity entity = new Entity(nextId);
                entity.name = prototypeName;
                nextId += 1;

                // copy all of the component field values
                foreach(Component comp in proto.Components()) {
                    Component.CopyOnto(comp, entity);
                }

                return entity;
            }

            return null;
        }

    }
}
