using System;

namespace RogueGame.Components {
    public class ApplyForceSkill : Component {

        public float force;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(ECompileSkillEffects), Event_DoSkillEffects, 1000);
            owner.AddEventResponse(typeof(EGetSkillDescription), Event_GetSkillDescr, 7000);
        }



        public bool Event_DoSkillEffects(ComponentEvent e) {
            var skillEffects = (ECompileSkillEffects)e;

            // for each target, send them away from the origin
            foreach(var combat in skillEffects.combats) {
                if(combat.defender == combat.attacker) {
                    continue;
                }

                Vector2 toCombat = combat.defender.position - combat.attacker.position;
                toCombat.Normalize();

                combat.defender.FireEvent(new EApplyForce() {
                    direction = toCombat,
                    force = force
                });
            }

            return true;
        }



        public bool Event_GetSkillDescr(ComponentEvent e) {
            var skillDescr = (EGetSkillDescription)e;
            
            skillDescr.description += "Applies %2" + force + "% force. ";
            return true;
        }

    }
}
