using System;
using RogueGame.Gameplay;

namespace RogueGame.Components {
    public class ResistanceComponent : Component {

        public string resistType;


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EDoDamage), Event_ResistDamage, 1000);
            owner.AddEventResponse(typeof(EGetResistance), Event_GetResistance);
        }


        public bool Event_ResistDamage(ComponentEvent e) {
            var damageEvent = (EDoDamage)e;

            // Use the combat engine to calculate the resistance based on the 
            damageEvent.damage = CombatEngine.CalcDamageResistance(
                damageEvent.damageType, 
                damageEvent.damage,
                resistType,
                out damageEvent.effectiveness);     
                   
            return true;
        }


        public bool Event_GetResistance(ComponentEvent e) {
            ((EGetResistance)e).resistanceType = resistType;            
            return true;
        }



    }
}
