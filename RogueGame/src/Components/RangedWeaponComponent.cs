using System.Collections.Generic;

namespace RogueGame.Components {
    public class RangedWeaponComponent : Component {

        public int maxRange;


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EGetStatList), Event_AddStatList, 500);
            owner.AddEventResponse(typeof(ECompileAttack), Event_CreateAttack, 10000);
            owner.AddEventResponse(typeof(EGetAttackRange), Event_GetAttackRange);
        }



        public virtual bool Event_AddStatList(ComponentEvent e) {
            List<string> stats = ((EGetStatList)e).stats;
            stats.Add("max range = " + maxRange);

            return true;
        }

        public virtual bool Event_GetAttackRange(ComponentEvent e) {
            var getAttackRange = (EGetAttackRange)e;
            getAttackRange.range = maxRange;
            return true;
        }


        public virtual bool Event_CreateAttack(ComponentEvent e) {
            var attack = (ECompileAttack)e;

            // check that the attack combat event is within the weapons range. If not, eat the attack event
            Vector2 combatDisplacement = attack.combat.defender.position - attack.combat.defender.position;
            if(combatDisplacement.Magnitude() > maxRange) {
                return false;
            }

            return true;
        }

    }
}
