

namespace RogueGame.Components {
    public class DoorComponent : Component {

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EOpen), Event_OnOpen, 0);
        }


        public bool Event_OnOpen(ComponentEvent e) {
            // if there is an openable component, it will have set this if successful
            bool wasOpened = ((EOpen)e).wasOpened;
            if(wasOpened) {
                // stop blocking los, and dont block movement
                owner.FireEvent(new ESetBlockState { blocking = false });
                owner.FireEvent(new ESetBlocksLOS { blocksLOS = false });
            }
            
            return true;
        }
    }
}
