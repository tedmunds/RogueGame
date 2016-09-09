# RogueGame
A simple Roguelike game with modern graphical styling and mouse input using the libtcod-net library. 

## The Entity-Component system

The engine is built in an entity-component system so that all gameplay entites are composed from specific components. These components
  commuinicate through a messaging event system, where each event is a unique class so that it's paramters are well defined. 
  
The idea is to make the components extremely decoupled so that they can be re-used to compose unique entities. As such, careful thought must be given to how a given behaviour should be broken down into components and events. 

For instance, the difference between a door and an item chest, and a locked version of each, is just a few different components. The openable component is used by both, while the door has a door component and the chest has a chest component and inventory component. The locked versions just add a lock component.

One of the keys to making this system work is that when an event is fired on an entity, its components respond to that event in a specific order. This order is determined by the priority that each component registers its listener to that type of event. Then, as a component processes an event, it has the option to eat it so that none of the lower priority listeners get to respond. In the prvious example, the lock component works for doors and chests because it simply eats the "open" event, and is registered with a high priority for that event so it goes before the other components. If later on, you go to add something that needs to respond before the lock component, you can simply register its event listener with a higher priority than the lock component does. 

The caveat with this system is that it makes it difficult to determine exactly what will happen when a given event is fired, especially since entites and their components are defined in arbitrary combinations in XML files. As such, it is important that components have minimal state and that their event listeners have minimal side effects, outside of modifying the event itself. 

## Design

This is a simple roguelike game with a lot fo the mechanics one expects from that genre. It is turn based, so whenever the player performs an action, the AI characters all get to perform a turn as well. The combat has two forms, bumping and ranged combat. Normally, to do a melee attack you simply walk into the character you want to attack. To do a ranged attack, you press the "inspect" button, which is space by default, and click on the character to attack. 

In order to make it a bit more interesting, you get to create your character before delving into the dungeon. In this process you can assign "attribute points" to your attributes which will impace how strong the palyer is at using different weapons and using certain skills. 

Skills are special abilities that get activated in game, and then perform some functionality. The most simple skill makes your next attack do extra damage. But so far there are skills that also knock back enemies or heal yourself, and the system is open to many more possibilities. 

To make the skill system deeper, each skill has an energy requirement and the palyer has a finite amount of energy that regenerates slowly each turn. This limits how often each skill can be used and forces the player to plan ahead in combat. 

## Defining Entities

Because the component system means that entities can have an arbitrary set of components with arbitrary initial values assigned to their fields, there needed to be a good way of defining them. The solution I came up with makes great use of C# reflection functionality and XML libraries. 

In the data/xml directory there are a few XMl files that contain lists of entites, where each entity defines a set of "parts". Each part will be decoded into a component once the game starts, and the xml attributes of the part willbe used to initialize the components public fields. Because C# has really powerful reflection tools, there is no need to set up a component to be compatible with this system. As soon as a new component class is created, you can add it to an entity via the xml system. 

I call the xml definitions "blueprints" and each blueprint gets decoded at runtime into a single entity instance. In game, these entities are kept seperate in a little dictionary called the prototype entities. When you want to spawn a new entity into the map, you identity its prototype by name and a deep copy of that prototype entity is created. In this way, with no input from the programmer required, an xml definition gets turned into a gameplay entity with arbitrary components, and as a result arbitrary functionality. 

The skills that are described in the Design section are actually defined as entites as well. They use a subset of the general components that respond to skillactivation events. As a result, defining skills is done in the same way as defining any other entity. By adding a single knockback skill component, any skill can be made to knock back whatever it targets. You could make a heal skill that knocks the player around with no extra code!
