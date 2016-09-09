using RogueGame.Gameplay;

namespace RogueGame.Components {
    /// <summary>
    /// Attack skills modify the users next attack in some way
    /// </summary>
    public class AttackSkill : Component {
        
        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            // inserts its useage mode with high priority
            owner.AddEventResponse(typeof(EActivateSkill), Event_GetUseMode, 1000);
            owner.AddEventResponse(typeof(EGetSkillDescription), Event_GetSkillDescr, 10000);
        }


        public bool Event_GetUseMode(ComponentEvent e) {
            ((EActivateSkill)e).useMode = SkillUseMode.EType.NextAttack;
            return true;
        }
        
        
        public bool Event_GetSkillDescr(ComponentEvent e) {
            var skillDescr = (EGetSkillDescription)e;
            skillDescr.description += "Next attack ";
            return true;
        }

    }
}
