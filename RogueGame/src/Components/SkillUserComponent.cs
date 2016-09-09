using System;
using System.Collections.Generic;
using RogueGame.Gameplay;

namespace RogueGame.Components {
    /// <summary>
    /// Component that allows entity to use skill type entities
    /// </summary>
    public class SkillUserComponent : Component {

        // params
        public int maxEnergy;
        public float regen;

        // dynamic state
        public List<Entity> skills = new List<Entity>();
        public int currentEnergy = 0;
        public Entity pendingAttackSkill;

        // essentially a timer that builds up evervy turn, by the regen rate, and adds one point to energy every time it hits 1.0
        private float nextEnergyPoint;

        
        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            currentEnergy = maxEnergy;

            owner.AddEventResponse(typeof(EOnSkillActivated),   Event_HandleSkillActivation);
            owner.AddEventResponse(typeof(EOnPerformAttack),    Event_OnAttack);
            owner.AddEventResponse(typeof(ERequestUseSkill),    Event_OnRequestSkillActivation);
            owner.AddEventResponse(typeof(ENewTurn),            Event_OnNewTurn);
        }


        public void AddNewSkill(Entity skill) {
            skills.Add(skill);
            skill.FireEvent(new ESkillEquipped() { user = owner });            
        }


        /// <summary>
        /// When a skill recieves an activation event, it will come back here, where it can be handled by the correct system.
        /// This is done because some skills use targeting UI / AI and some use attack events and some are immediate
        /// </summary>
        public bool Event_HandleSkillActivation(ComponentEvent e) {
            var activationEvent = (EOnSkillActivated)e;
            Entity skill = activationEvent.skill;

            // check how much energy the skill uses, and dont let it be used if its too much
            var checkSkillReq = (EGetEnergy)skill.FireEvent(new EGetEnergy());
            if(checkSkillReq.requiredEnergy > currentEnergy) {
                return true;
            }

            bool skillActivated = false;

            // Handle different use mode cases. 
            switch(activationEvent.useMode) {
                case SkillUseMode.EType.NextAttack:
                    // for now, only one pending attack skill can be active at a time
                    if(pendingAttackSkill == null) {
                        pendingAttackSkill = skill;
                        skillActivated = true;
                    }
                    else if(pendingAttackSkill == skill) {
                        // if the same skill is re-activated, cancel it
                        pendingAttackSkill = null;
                    }

                    break;
                case SkillUseMode.EType.Targeted:
                    // TODO: delegate this somehow to UI or AI to select the target location / entity
                    break;
                case SkillUseMode.EType.Self:
                    // use immediatly on self
                    skillActivated = true;
                    float attributeModifier = CombatEngine.GetEntitySkillStrength(owner, skill);

                    var skillEffects = new ECompileSkillEffects() {
                        user = owner,
                        skillStrength = attributeModifier,
                        baseLocation = owner.position,
                    };

                    skillEffects.combats.Add(new CombatInstance(owner, owner) { { "skill", skill } });
                    skill.FireEvent(skillEffects);

                    // process the events
                    foreach(var combat in skillEffects.combats) {
                        CombatEngine.ProcessCombat(combat);
                    }

                    break;
            }

            // if the skill was activated, reduce the energy level
            if(skillActivated) {                
                currentEnergy -= checkSkillReq.requiredEnergy;
                nextEnergyPoint = 0;
            }
            
            return true;
        }


        
        public bool Event_OnRequestSkillActivation(ComponentEvent e) {
            var activationRequest = (ERequestUseSkill)e;
            int idx = activationRequest.slot;
            
            if(idx >= 0 && idx < skills.Count) {
                skills[idx].FireEvent(new EActivateSkill() { activator = owner });
            }

            return true;
        }


        public bool Event_OnAttack(ComponentEvent e) {
            var baseAttack = (EOnPerformAttack)e;
            
            // if there was a skill waiting to be used as an attack modifier, pass along this attack event to be modified
            if(pendingAttackSkill != null) {
                float attributeModifier = CombatEngine.GetEntitySkillStrength(owner, pendingAttackSkill);
                
                // add the fact that it was a skill being used to the original combat
                baseAttack.damageEvent.combat.Set("skill", pendingAttackSkill);

                ECompileSkillEffects compileSkillEvent = new ECompileSkillEffects() {
                    user = owner,
                    skillStrength = attributeModifier,
                    baseLocation = baseAttack.damageEvent.combat.defender.position,                    
                };
                compileSkillEvent.combats.Add(baseAttack.damageEvent.combat);
                
                pendingAttackSkill.FireEvent(compileSkillEvent);

                foreach(var combat in compileSkillEvent.combats) {
                    CombatEngine.ProcessCombat(combat);
                }
                
                // check if the skill is done
                var skillCompleted = (EGetSkillCompleted)pendingAttackSkill.FireEvent(new EGetSkillCompleted() { isCompleted = true });

                // and reset the skill 
                if(skillCompleted.isCompleted) {
                    pendingAttackSkill = null;
                }

                // set the base attack state to not apply damage, because the skill handles it
                baseAttack.canPerform = false;
            }
            
            return true;
        }


        public bool Event_OnNewTurn(ComponentEvent e) {
            // On a new turn, energy regenerates a bit. Check some conditions to gate regen
            if(currentEnergy < maxEnergy && pendingAttackSkill == null) {
                nextEnergyPoint += regen;
            }

            // if the regen level has build up to one, reset it and add an energy point
            if(nextEnergyPoint >= 1.0f) {
                nextEnergyPoint = 0.0f;
                currentEnergy += 1;
            }

            return true;
        }



    }
}
