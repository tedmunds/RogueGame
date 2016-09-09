using RogueGame.Gameplay;

namespace RogueGame.Components {
    public class IncreaseDamageSkill : Component {

        public int baseDamage;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(ECompileSkillEffects), Event_DoSkillEffects, 1000);
            owner.AddEventResponse(typeof(EGetSkillDescription), Event_GetSkillDescr, 7500);
        }

        
        public bool Event_DoSkillEffects(ComponentEvent e) {
            var skillEffects = (ECompileSkillEffects)e;

            int damageIncr = (int)(skillEffects.skillStrength * baseDamage);

            // go through each of the effected, and apply the damage events
            foreach(var combat in skillEffects.combats) {
                int currDmg = combat.Get<int>("damage");
                combat.Set("damage", currDmg + damageIncr);
            }

            return true;
        }



        public bool Event_GetSkillDescr(ComponentEvent e) {
            var skillDescr = (EGetSkillDescription)e;

            // calculate the damage increase based on attributes so its reflected in description
            float modifier = CombatEngine.GetEntitySkillStrength(null, owner);
            int val = (int)(modifier * baseDamage);
            skillDescr.description += "Does Damage %2+" + val + "%. ";
            return true;
        }

    }
}
