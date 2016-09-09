using System;

namespace RogueGame.Components {
    /// <summary>
    /// Component is the base of composition for the entity class. Can receive and handle events as
    /// a method of communicating intent to other components and systems, without any coupling to those
    /// systems or components.
    /// 
    /// <para>
    /// An Event is activeted by calling the FireEvent() method on the entity owner of the component. 
    /// The component also implements a FireEvent method, but this is only used by the entity owner to pass
    /// along events and should never be called explicitly.
    /// </para>
    /// 
    /// Components register specific methods to specfic Events in the OnAttach(Entity owner) method. This is called
    /// when the component is attached to an entity, and the owner.AddEventResponse method is used to set up a response.
    /// </summary>
    public abstract class Component {        

        /// <summary>
        /// The entity that this component is attached to
        /// </summary>
        public Entity owner;
        
        public Component() {}
        
        /// <summary>
        /// Called when this component instance is attached to an entity
        /// </summary>
        public virtual void OnAttach(Entity owner) {
            this.owner = owner;
        }


        /// <summary>
        /// Duplicate the master component onto the input gameObject (deep copy), and returns the newly added component. 
        /// The returned component has already been properly attached to the input entity and nothing else needs to be done.
        /// </summary>
        public static Component CopyOnto(Component master, Entity entity) {
            Type masterTypeInfo = master.GetType();

            object copied = Activator.CreateInstance(masterTypeInfo);

            foreach(var field in masterTypeInfo.GetFields()) {
                // Do not try to copy constants
                if(field.IsLiteral) {
                    continue;
                }
                object masterVal = field.GetValue(master);
                field.SetValue(copied, masterVal);
            }

            Component newComp = (Component)copied;

            // finally, assign the input game object to this component
            entity.AddComponent(newComp);
            return newComp;
        }



    }
}
