using System;


namespace RogueGame.Components {

    public class EquipmentComponent : Component {
        
        public Entity weapon;


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            
            owner.AddEventResponse(typeof(EEquip), Event_OnEquip);
            owner.AddEventResponse(typeof(ECompileAttack), Event_CreateAttack, 1000);
            owner.AddEventResponse(typeof(EGetAttackRange), Event_GetAttackRange);
        }


        public bool Event_OnEquip(ComponentEvent e) {
            bool wasEquipped = false;

            var equipEvent = (EEquip)e;

            // get the item payload of the event and check which slot it should be equipped in
            Entity newItem = equipEvent.item;

            // ask the item if it is a weapon
            var canEquipEvent = (ECanEquip)newItem.FireEvent(new ECanEquip() {
                equipSlot = "weapon"
            });

            // make sure that the item is only equipped as weapon if there isnt already a weapon
            if(canEquipEvent.canEquip && weapon == null) {
                weapon = newItem;
                wasEquipped = true;
            }

            // and record the fact that it was equipped in the calling event
            if(wasEquipped) {
                equipEvent.wasEquipped = true;
            }

            return true;
        }


        public bool Event_CreateAttack(ComponentEvent e) {
            // just pass the attack building responsibility on to the weapon itself
            if(weapon != null) {
                weapon.FireEvent(e);
            }

            return true;
        }


        
        public bool Event_GetAttackRange(ComponentEvent e) {
            // pass the range event on to the equipped weapon
            if(weapon != null) {
                weapon.FireEvent(e);
            }
            return true;
        }
    }
}
