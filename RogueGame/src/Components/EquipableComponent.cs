using System;


namespace RogueGame.Components {
    public class EquipableComponent : Component {

        public string slot;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            owner.AddEventResponse(typeof(ECanEquip), Event_CanEquip);
        }



        public bool Event_CanEquip(ComponentEvent e) {
            var equipEvent = (ECanEquip)e;
            string targetSlot = equipEvent.equipSlot;

            // if the target slot matches this entites slot then it can be equipped
            if(targetSlot.Equals(slot)) {
                equipEvent.canEquip = true;
            }

            return true;
        }
    }
}
