using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using RogueGame.Components;

namespace RogueGame {

    /// <summary>
    /// Entity is a lightweight bag of components. The entity contains little functionaility beyond holding arbitrary components, 
    /// and firing events to them.
    /// </summary>
    [Serializable()]
    public class Entity : ISerializable {
        
        // prototype of method that recieves a single type of event
        public delegate bool EventHandler(ComponentEvent e);

        private struct EventResponse {
            public EventHandler handler;
            public int priority;
        }
        
        /// <summary>
        /// completely unique number for this entity
        /// </summary>
        public int id;

        /// <summary>
        /// Name of the entity, not screen name just to seperate it from other entitiy types
        /// </summary>
        public string name;

        /// <summary>
        /// Entities position in the world
        /// </summary>
        public Vector2 position;
        
        private List<Component> components;
        
        private Dictionary<Type, List<EventResponse>> eventResponse;

        public Entity(int id) {
            this.id = id;

            // init to size 0, since these are only added to at creation so resizing isnt a problem
            components = new List<Component>(0);
            eventResponse = new Dictionary<Type, List<EventResponse>>(0);
        }

        /// <summary>
        /// Registers a components callback method for the input event type. Whenever that type of event is fired on this entity, 
        /// the callback will be fired, with the event instance passed in as a parameter
        /// </summary>
        public void AddEventResponse(Type eventType, EventHandler handler, int priority = 0) {
            List<EventResponse> responses;
            
            // Find the event response list for this event type. If no events of this type are registered, create a new response list
            if(!eventResponse.TryGetValue(eventType, out responses)) {
                responses = new List<EventResponse>(1);
                eventResponse.Add(eventType, responses);
            }

            EventResponse r = new EventResponse();
            r.handler = handler;
            r.priority = priority;

            // figure out what index to add the new response, given the components rank. Components are garenteed ordering by priority
            int i;
            for(i = 0; i < responses.Count; i++) {
                if(priority > responses[i].priority) {
                    break;
                }
            }

            responses.Insert(i, r);
        }




        /// <summary>
        /// returns the component of type T on this entity, if one exists. Null otherwise
        /// </summary>
        public T Get<T>() where T : Component {
            foreach(Component c in components) {
                if(c is T) {
                    return (T)c;
                }
            }

            return null;
        }

        /// <summary>
        /// returns the component of input type on this entity, if one exists. Null otherwise
        /// </summary>
        public Component Get(Type componentType) {
            foreach(Component c in components) {
                if(c.GetType() == componentType) {
                    return c;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if this entity has a component of the input type
        /// </summary>
        public bool Has(Type componentType) {
            foreach(Component c in components) {
                if(c.GetType() == componentType) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds the input component instance to this entity, and calls the components OnAttach method
        /// </summary>
        /// <param name="comp"></param>
        public void AddComponent(Component comp) {
            components.Add(comp);
            comp.OnAttach(this);
        }


        /// <summary>
        /// Removes the input component instance from the entity, and un registers all of it's event listeners
        /// </summary>
        public void RemoveComponent(Component comp) {
            components.Remove(comp);

            // remove all of the components registered events
            foreach(var reponses in eventResponse.Values) {
                foreach(var r in reponses) {
                    if(r.handler.Target == comp) {
                        reponses.Remove(r);
                    }
                }
            }            
        }
        

        /// <summary>
        /// Pushes the input event to all the components that can respond to it on this entity.
        /// Returns the input event. NOTE: Its params may have been modified.
        /// </summary>
        public ComponentEvent FireEvent(ComponentEvent e) {
            bool eventActive = true;
            List<EventResponse> handlers;

            // get the set of handlers for this event type, and respond to them in order. Order is IMPORTANT
            if(eventResponse.TryGetValue(e.GetType(), out handlers)) {
                foreach(EventResponse respond in handlers) {
                    eventActive = respond.handler(e);

                    // this indicates the event has been consumed by the components handler, so no one else gest to respond to it
                    if(!eventActive) {
                        break;
                    }
                }
            }

            return e;
        }


        /// <summary>
        /// Iterator for component list of this entity
        /// </summary>
        public IEnumerable<Component> Components() {
            foreach(Component c in components) {
                yield return c;
            }
        }

        // =================================================================================================================================================================================================================
        // Serialization handling: 
        // When an entity is serialized, its components are serialized in a special way so that they can be properly registered and set up with no additional markup per component
        // =================================================================================================================================================================================================================

        [Serializable()]
        private struct SerializeComponentData {
            public string compType;
            public List<KeyValuePair<string, object>> fields;
            public SerializeComponentData(string compType) {
                this.compType = compType;
                this.fields = new List<KeyValuePair<string, object>>();
            }
        }

        // Implement this method to serialize data. The method is called on serialization.
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            info.AddValue("id", id, typeof(int));
            info.AddValue("name", name, typeof(string));
            info.AddValue("position", position, typeof(Vector2));

            // handle components
            List<SerializeComponentData> componentData = new List<SerializeComponentData>(components.Count);
            foreach(Component comp in components) {
                Type compType = comp.GetType();
                var compData = new SerializeComponentData(compType.Name);

                foreach(var field in compType.GetFields()) {
                    // Do not try to copy constants
                    if(field.IsLiteral) {
                        continue;
                    }
                    
                    object fieldVal = field.GetValue(comp);
                    string type = field.Name;

                    // Have to handle colours as special case since they cannot be serialized
                    if(field.FieldType == typeof(libtcod.TCODColor) && fieldVal != null) {
                        fieldVal = Utilities.ColorConverter.ColorToHex((libtcod.TCODColor)fieldVal);
                        type = typeof(string).Name;
                    }

                    compData.fields.Add(new KeyValuePair<string, object>(field.Name, fieldVal));
                }

                componentData.Add(compData);
            }

            info.AddValue("componentData", componentData);
        }



        // The special constructor is used to deserialize values.
        public Entity(SerializationInfo info, StreamingContext context) {
            const string ROGUEGAME_COMPONENT_NAMESPACE = "RogueGame.Components.";

            // normal part of constructor
            components = new List<Component>(0);
            eventResponse = new Dictionary<Type, List<EventResponse>>(0);

            id = (int)info.GetValue("id", typeof(int));
            name = (string)info.GetValue("name", typeof(string));
            position = (Vector2)info.GetValue("position", typeof(Vector2));

            // handle components
            var componentData = (List<SerializeComponentData>)info.GetValue("componentData", typeof(List<SerializeComponentData>));
            foreach(var compData in componentData) {
                Type componentType = Type.GetType(ROGUEGAME_COMPONENT_NAMESPACE + compData.compType);
                object newComponent = Activator.CreateInstance(componentType);

                foreach(var fieldData in compData.fields) {
                    string fieldName = fieldData.Key;
                    var fieldInfo = componentType.GetField(fieldName);
                    object dataVal = fieldData.Value;

                    // If the type of the field being assigned is color, do the opposite of the conversion done in serisalization
                    if(fieldInfo.FieldType == typeof(libtcod.TCODColor) && fieldData.Value != null) {
                        dataVal = Utilities.ColorConverter.HexToColor((string)fieldData.Value);
                    }
                    
                    fieldInfo.SetValue(newComponent, dataVal);
                }

                // component has been deserialized using the cutom system, now add it to the entity using the proper functions,
                // this will properly register the component and set up event responses
                AddComponent((Component)newComponent);
            }
        }


    }
}
