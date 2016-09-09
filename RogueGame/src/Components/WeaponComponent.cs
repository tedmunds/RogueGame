using System;
using System.Collections.Generic;

namespace RogueGame.Components {


    public class WeaponComponent : Component {

        // params
        public string damageType;
        public string attribute;
        public int baseDamage;



        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EGetStatList), Event_AddStatList, 1000);
            owner.AddEventResponse(typeof(ECompileAttack), Event_CreateAttack);
        }



        public virtual bool Event_AddStatList(ComponentEvent e) { 
            List<string> stats = ((EGetStatList)e).stats;
            stats.Add("damage type = " + damageType);
            stats.Add("attribute   = " + attribute);
            stats.Add("base damage = " + baseDamage);

            return true;
        }



        public virtual bool Event_CreateAttack(ComponentEvent e) {
            var attack = (ECompileAttack)e;

            // TODO: add modifiers for skill level and damage type etc. damage
            int attackDmg = attack.combat.Get<int>("damage");
            attack.combat.Set("damage", attackDmg + baseDamage);
            attack.combat.Set("weapon", owner);
            attack.combat.Set("damageType", damageType);

            return true;
        }

    }
}
