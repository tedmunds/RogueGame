using System;
using RogueGame.Map;

namespace RogueGame.Components {

    /// <summary>
    /// Portal tells the world to load new map, based on content of this component
    /// </summary>
    public class PortalComponent : Component {

        // Can be a constant like PREVIOUS, or DUNGEON_NEXT or DUNGEON_ENTER or DUNGEON_EXIT
        public string mapCode;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EActivate), Event_Activation);
        }


        public bool Event_Activation(ComponentEvent e) {
            // tell the world about the code on activation
            World world = Engine.instance.world;
            world.GotoNewLevel(mapCode);

            return true; 
        }


    }
}
