using System;
using RogueGame.Map;
using RogueGame.Gameplay;

namespace RogueGame.Components {
    public class AreaOfEffectSkill : Component {

        public float radius;
        
        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            
            owner.AddEventResponse(typeof(ECompileSkillEffects), Event_DoSkillEffects, 5000);
            owner.AddEventResponse(typeof(EGetSkillDescription), Event_GetSkillDescr, 3000);
        }

        
        public bool Event_DoSkillEffects(ComponentEvent e) {
            var skillEffects = (ECompileSkillEffects)e;

            // get all of the entities in the radius, and add them to the effected entities
            AreaMap map = Engine.instance.world.currentMap;
            Entity[] hits = map.GetAllObjectsInRadius(skillEffects.baseLocation, radius);
            
            // add each, but also make sure not to repeat any
            foreach(Entity hit in hits) {
                bool newTarget = true;
                foreach(var combat in skillEffects.combats) {
                    if(combat.defender == hit) {
                        newTarget = false;
                    }
                }

                // the list did not contain this target yet, so add an attack for it
                if(newTarget) {
                    skillEffects.combats.Add(new CombatInstance(skillEffects.user, hit) {
                        { "skill", owner }
                    });
                }
            }

            return true;
        }


        public bool Event_GetSkillDescr(ComponentEvent e) {
            var skillDescr = (EGetSkillDescription)e;
            skillDescr.description += "Hits all enemies in %2" + ((int)radius) + "% tiles. ";
            return true;
        }


    }
}
