using System;


namespace RogueGame.Components {

    public class ChestComponent : Component {


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EOpen), Event_OnOpen, 0);
            owner.AddEventResponse(typeof(EDied), Event_OnOpen, 0);
        }


        public bool Event_OnOpen(ComponentEvent e) {
            // if the chest is opened, dump its inventory
            owner.FireEvent(new EDropAllItems());
            return true;
        }
    }
}
