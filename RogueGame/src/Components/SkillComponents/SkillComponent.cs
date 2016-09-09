using System;
using libtcod;

namespace RogueGame.Components {
    /// <summary>
    /// Base skill component that defines an entity as a "skill" that can be sactivated by an entity to do a thing.
    /// An entity The skill component talks to [...]Skill named components that use [...]Skill named events to talk to
    /// eachother
    /// </summary>
    public class SkillComponent : Component {

        public int energy;
        public string name;
        public string attribute;

        // the entity this skill is equipped by
        public Entity skillUser;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            owner.AddEventResponse(typeof(EGetScreenName),      Event_GetName);
            owner.AddEventResponse(typeof(EActivateSkill),      Event_OnActivtivation);
            owner.AddEventResponse(typeof(EGetEnergy),          Event_GetRequiredEnergy);
            owner.AddEventResponse(typeof(EGetSkillAttribute),  Event_GetAttribute);
            owner.AddEventResponse(typeof(EGetSkillUser),       Event_GetSkillUser);
            owner.AddEventResponse(typeof(ESkillEquipped),      Event_OnSkillEquipped);
        }



        public bool Event_GetName(ComponentEvent e) {
            ((EGetScreenName)e).text += name;
            return true;
        }

        
        public bool Event_GetSkillUser(ComponentEvent e) {
            ((EGetSkillUser)e).user = skillUser;
            return true;
        }

        public bool Event_OnSkillEquipped(ComponentEvent e) {
            skillUser = ((ESkillEquipped)e).user;
            return true;
        }


        public bool Event_OnActivtivation(ComponentEvent e) {
            var activationEvent = (EActivateSkill)e;
            Entity activator = activationEvent.activator;

            // Pass the skill event onto the user, which can handle the next stages of activation
            activator.FireEvent(new EOnSkillActivated() {
                useMode = activationEvent.useMode,
                skill = owner
            });

            return true;
        }

        
        public bool Event_GetRequiredEnergy(ComponentEvent e) {
            ((EGetEnergy)e).requiredEnergy += energy;
            return true;
        }


        public bool Event_GetAttribute(ComponentEvent e) {
            ((EGetSkillAttribute)e).targetAttribute = attribute;
            return true;
        }



    }
}
