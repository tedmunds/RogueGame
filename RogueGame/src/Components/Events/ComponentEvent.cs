
namespace RogueGame.Components {
    /// <summary>
    /// The base class for the components messaging system. Events are very minimal objects with default constructors and few fields. They
    /// should never have any methods. 
    /// 
    /// <para>
    /// Events are passed to an entity through the FireEvent method, and should never by fired directly on a component.
    /// The Entity will pass the event to all components that are registered to listen to the input type of event.
    /// </para>
    /// <para>
    /// The fields of an event will be modified by the components who respond to it, so that an event is used to pass messages
    /// to and from an entity and between components.
    /// </para>
    /// </summary>
    public abstract class ComponentEvent {        
    }
}
