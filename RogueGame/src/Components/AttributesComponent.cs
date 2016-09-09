using System;
using System.Collections.Generic;
using System.Reflection;

namespace RogueGame.Components {
    /// <summary>
    /// Attributes define the entites skill at doing different actions int he game. Things like
    /// weapons and skills look up attribute levels to modifer their effects
    /// </summary>
    public class AttributesComponent : Component {

        // general stats
        [Stat] public int strength;
        [Stat] public int wisdom;
        [Stat] public int agility;

        // weapon stats
        [Stat] public int blades;
        [Stat] public int hammers;
        [Stat] public int polearms;
        [Stat] public int shields;
        [Stat] public int bows;
        [Stat] public int crossbows;
        [Stat] public int throwing;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EGetAttributeLevel), Event_GetAttrLevel, 1000);
        }


        public bool Event_GetAttrLevel(ComponentEvent e) {
            var getAttr = (EGetAttributeLevel)e;
            string targetAttr = getAttr.target;

            // the value of the attribute, to be discovered
            int val = 0;
            bool foundAttr = false;

            // use reflection to get the value of the attrbutes
            FieldInfo[] attributeFields = GetType().GetFields();
            foreach(FieldInfo field in attributeFields) {
                if(field.GetCustomAttribute<StatAttribute>() != null && 
                    field.Name.Equals(targetAttr)) {
                    // Found the attribute, get the integer value
                    val = (int)field.GetValue(this);
                    foundAttr = true;
                    break;
                }
            }

            if(!foundAttr) {
                Console.WriteLine("WARNING: AttributesComponent::Event_GetAttrLevel [" + targetAttr + "] does not exist.");
            }

            // send the value back in the event
            getAttr.level = val;
            return true;
        }


        /// <summary>
        /// Iterates over the name of each attributes
        /// </summary>
        public IEnumerable<string> Attributes() {
            FieldInfo[] attributeFields = GetType().GetFields();

            foreach(FieldInfo field in attributeFields) {
                if(field.GetCustomAttribute<StatAttribute>() != null) {
                    yield return field.Name;
                }
            }
        }

        /// <summary>
        /// Iterates over the the field of each attribute
        /// </summary>
        public IEnumerable<FieldInfo> AttributeFields() {
            FieldInfo[] attributeFields = GetType().GetFields();

            foreach(FieldInfo field in attributeFields) {
                if(field.GetCustomAttribute<StatAttribute>() != null) {
                    yield return field;
                }
            }
        }



    }
}
