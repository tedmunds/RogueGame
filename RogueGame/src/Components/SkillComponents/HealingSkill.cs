using System;
using RogueGame.Gameplay;

namespace RogueGame.Components {
    public class HealingSkill : Component {

        public int healing;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(ECompileSkillEffects), Event_OnCompile);
            owner.AddEventResponse(typeof(EGetSkillDescription), Event_GetSkillDescr, 7500);
        }



        public bool Event_OnCompile(ComponentEvent e) {
            var skillEffects = (ECompileSkillEffects)e;

            int healAmount = (int)(skillEffects.skillStrength * healing);

            // add some healing to each combat instance
            foreach(var combat in skillEffects.combats) {
                combat.Add("healing", healAmount);
            }

            return true;
        }


        public bool Event_GetSkillDescr(ComponentEvent e) {
            var skillDescr = (EGetSkillDescription)e;

            // calculate the damage increase based on attributes so its reflected in description
            float modifier = CombatEngine.GetEntitySkillStrength(null, owner);
            int val = (int)(modifier * healing);
            skillDescr.description += "Heals %2+" + val + "% points of damage. ";
            return true;
        }

    }
}
