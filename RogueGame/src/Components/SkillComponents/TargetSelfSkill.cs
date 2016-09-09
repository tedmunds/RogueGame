using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RogueGame.Components {
    public class TargetSelfSkill : Component {


        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EActivateSkill), Event_GetUseMode, 1000);
            owner.AddEventResponse(typeof(EGetSkillDescription), Event_GetSkillDescr, 10000);
        }



        public bool Event_GetUseMode(ComponentEvent e) {
            ((EActivateSkill)e).useMode = SkillUseMode.EType.Self;
            return true;
        }


        public bool Event_GetSkillDescr(ComponentEvent e) {
            var skillDescr = (EGetSkillDescription)e;
            skillDescr.description += "";
            return true;
        }



    }
}
