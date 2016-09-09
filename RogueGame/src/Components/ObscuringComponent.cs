using System;


namespace RogueGame.Components {
    public class ObscuringComponent : Component {

        public bool blocking = true;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            owner.AddEventResponse(typeof(EGetBlocksLOS), Event_BlocksLOS);
            owner.AddEventResponse(typeof(ESetBlocksLOS), Event_SetBlocksLOS);
        }


        public bool Event_BlocksLOS(ComponentEvent e) {
            ((EGetBlocksLOS)e).blocksLOS = blocking;
            return true;
        }

        public bool Event_SetBlocksLOS(ComponentEvent e) {
            blocking = ((ESetBlocksLOS)e).blocksLOS;
            return true;
        }


    }
}
