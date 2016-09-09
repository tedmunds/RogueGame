using System;
using System.Xml;
using System.Reflection;
using libtcod;
using RogueGame.Components;

namespace RogueGame.Data {

    public class BlueprintLoader {

        private const char DATA_CHAR_SYMBOL = '%';

        /// <summary>
        /// loads a blueprint file and innits a set of entity prototypes, with all of their values intialized
        /// </summary>
        public static Entity[] LoadBlueprintSet(string blueprintFile) {
            var blueprintSet = XMLObjectLoader.LoadXMLObject<BlueprintSet>("data\\xml\\" + blueprintFile, null);
            if(blueprintSet == null) {
                return null;
            }

            Entity[] entities = new Entity[blueprintSet.blueprints.Length];

            // decode each entities blueprint xml into an actual object
            for(int i = 0; i < blueprintSet.blueprints.Length; i++) {
                entities[i] = CreateEntityFromBlueprint(entities, blueprintSet.blueprints[i]);
            }

            return entities;
        }



        private static Entity CreateEntityFromBlueprint(Entity[] entityList, EntityBlueprint blueprint) {
            Entity entity = new Entity(-1);
            entity.name = blueprint.name;

            // if it has a parent, first load the parent values
            if(blueprint.parent != null) {
                InitFromParentBlueprint(entityList, entity, blueprint.parent);
            }

            // then add all of the entities new components
            foreach(BlueprintPart part in blueprint.parts) {
                InterpretComponent(entity, part);
            }

            Console.WriteLine("BlueprintLoader - Loaded [" + entity.name + "]");
            return entity;
        }



        /// <summary>
        /// Interprets a single blueprint part into an actual component obejct instance, setting all of its fields based on the blueprint.
        /// It also handles parent blueprints already having created components, by simply overriding field values.\n
        /// NOTE: does not add the component to the entity
        /// </summary>
        private static void InterpretComponent(Entity entity, BlueprintPart part) {
            Type componentType = Type.GetType("RogueGame.Components." + part.type);            
            if(componentType == null) {
                Console.WriteLine("*** ERROR: BlueprintLoader - Can't create component type: [" + part.type + "] on [" + entity.name + "]");
                return;
            }
            
            object newComp = null;
            bool modifiedParentComponent = false;

            // if the entity already has this component, this part will override it's param values
            if(entity.Has(componentType)) {
                newComp = entity.Get(componentType);
                modifiedParentComponent = true;
            }
            else {
                newComp = Activator.CreateInstance(componentType);
            }

            // parse each attribute as a parameter now, guessing which type it is meant to be
            if(part.parameters != null) {
                foreach(XmlAttribute arg in part.parameters) {
                    FieldInfo componentField = componentType.GetField(arg.LocalName);

                    if(componentField == null) {
                        Console.WriteLine("*** ERROR: BlueprintLoader - [" + part.type + "] does not have field: [" + arg.LocalName + "] on [" + entity.name + "]");
                        continue;
                    }

                    // get ready to interpret the type of the field
                    Type fieldType = componentField.FieldType;
                    object fieldValue = null;

                    // there is a special case for characters so that numbers can be loaded as chars as well (like bytes)
                    if(fieldType == typeof(Char)) {
                        fieldValue = GetCharFromData(arg.InnerText);
                    }
                    // special case for colrs, since there are represented in data as hex codes, but need to be converted to a tcodcolor struct
                    else if (fieldType == typeof(TCODColor)){
                        fieldValue = Utilities.ColorConverter.HexToColor(arg.InnerText);
                    }
                    // all other types can be converted in their own generic way
                    else {
                        var typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(fieldType);
                        fieldValue = typeConverter.ConvertFromString(arg.InnerText);
                    }

                    // Finally assign the value to the component instance's actual field
                    if(fieldValue == null) {
                        Console.WriteLine("*** ERROR: BlueprintLoader - Couldn't get field data for field [" + arg.LocalName + "] in [" + part.type + "] on [" + entity.name + "]. Current value = " + arg.InnerText);
                    }
                    else {
                        componentField.SetValue(newComp, fieldValue);
                    }
                }
            }

            // if it wasnt alrady on the entity from the parent, then add it now
            if(!modifiedParentComponent) {
                entity.AddComponent((Component)newComp);
            }
        }



        private static void InitFromParentBlueprint(Entity[] previous, Entity entity, string parentName) {
            Entity parent = null;

            // look throught he previously loaded entities
            for(int i = 0; i < previous.Length; i++) {
                if(previous[i] == null) {
                    continue;
                }

                if(previous[i].name == parentName) {
                    parent = previous[i];
                    break;
                }
            }

            if(parent == null) {
                Console.WriteLine("*** ERROR: BlueprintLoader - could not find parent: [" + parentName + "] on [" + entity.name + "]");
                return;
            }

            // Add all of the parent prototypes components to the new prototype
            foreach(Component parentComp in parent.Components()) {
                Component.CopyOnto(parentComp, entity);
            }
        }



        /// <summary>
        /// takes the input string of either one char or an int (the size of a byte) and converts it to a char
        /// </summary>
        private static char GetCharFromData(string data) {
            // if the first character of the data is the special char, treat it as a character, otherwise treat it as a number
            if(data.Length >= 1 && data[0] == DATA_CHAR_SYMBOL) {
                var typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(System.Char));
                return (char)typeConverter.ConvertFromString(data.Substring(1));
            }
            else {
                var typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(System.Int32));
                object val = typeConverter.ConvertFromString(data);
                int intVal = (int)val;
                char charVal = (char)intVal;
                return charVal;
            }
        }
        
    }
}
