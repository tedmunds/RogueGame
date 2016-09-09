using System;
using System.Collections;
using System.Collections.Generic;
using libtcod;
using RogueGame.Components;
using RogueGame.Map;
using RogueGame.Interface;

namespace RogueGame.Gameplay {
    
    /// <summary>
    /// A single combat event where one entity attacks another entity. 
    /// </summary>
    public sealed class CombatInstance : IEnumerable {
        public Entity attacker;
        public Entity defender;

        private Dictionary<string, object> events = new Dictionary<string, object>();

        public CombatInstance(Entity attacker, Entity defender) {
            this.attacker = attacker;
            this.defender = defender;
        }

        /// <summary>
        /// Gets the combat instance attribute of the given name
        /// </summary>
        public T Get<T>(string attr) {
            object val;
            if(events.TryGetValue(attr, out val)) {
                return (T)val;
            }

            return default(T);
        }

        /// <summary>
        /// Sets the attribute in the combat instance, or adds it if it doesn't exist yet
        /// </summary>
        public void Set(string attr, object val) {
            if(events.ContainsKey(attr)) {
                events[attr] = val;
            }
            else {
                events.Add(attr, val);
            }
        }

        // Allows for events to be initialized with an initializer list, for convenience
        public void Add(string key, object value) {
            events.Add(key, value);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)events).GetEnumerator();
        }
    }


    /// <summary>
    /// Keeps all of the combat functionality inone place for easier tuning. 
    /// </summary>
    public abstract class CombatEngine {
        
        /// <summary>
        /// Processes a single attack
        /// </summary>
        public static void ProcessCombat(CombatInstance combatInstance) {            
            int damage = combatInstance.Get<int>("damage");
            int healing = combatInstance.Get<int>("healing");

            if(damage > 0) {
                ProcessDamageEvent(combatInstance);
            }
            if(healing > 0) {
                ProcessHealingEvent(combatInstance);
            }
        }



        /// <summary>
        /// Process the combat as a damage event
        /// </summary>
        private static void ProcessDamageEvent(CombatInstance combatInstance) {
            Engine engine = Engine.instance;
            Entity player = engine.playerController.player;
            
            // First check if this is a valid attacker / defender combo
            var canAttack = (ECanAttack)combatInstance.defender.FireEvent(new ECanAttack() {
                asker = combatInstance.attacker,
                validTarget = true
            });

            if(!canAttack.validTarget) {
                return;
            }
            
            // get the names of the actors before they are updated, so it never says you attacked a dead thing
            var getAttackerName = (EGetScreenName)combatInstance.attacker.FireEvent(new EGetScreenName());
            var getDefenderName = (EGetScreenName)combatInstance.defender.FireEvent(new EGetScreenName());

            // check if there was some sort of tool used in the attack, and get its name
            string toolUsed = "";
            if(combatInstance.Get<Entity>("weapon") != null) {
                Entity weapon = combatInstance.Get<Entity>("weapon");
                var getWeaponName = (EGetScreenName)weapon.FireEvent(new EGetScreenName());
                toolUsed = getWeaponName.text;
            }

            // skill name overrides weapon name in message printout
            if(combatInstance.Get<Entity>("skill") != null) {
                Entity skill = combatInstance.Get<Entity>("skill");
                var getSkillName = (EGetScreenName)skill.FireEvent(new EGetScreenName());
                toolUsed = getSkillName.text;
            }

            // apply the damage, will also calculate resistances etc.
            var damageEvent = (EDoDamage)combatInstance.defender.FireEvent(new EDoDamage() {
                damage = combatInstance.Get<int>("damage"),
                damageType = combatInstance.Get<string>("damageType"),
                instigator = combatInstance.attacker,
                effectiveness = 0  // the default effectiveness, in case it does not resist at all
            });

            string effectiveness = GetEffectivenessString(damageEvent.effectiveness, damageEvent.damage, combatInstance.defender);

            //Engine.LogMessage("%2" + getAttackerName.text + "% hits %3" + getDefenderName.text + "% for %1" + attackDamage + "% damage", combatInstance.defender);
            if(toolUsed != "") {
                Engine.LogMessage("%2" + getAttackerName.text + "% hits %3" + getDefenderName.text + "% with %1" + toolUsed + "%" + effectiveness,
                    combatInstance.defender);
            }
            else {
                Engine.LogMessage("%2" + getAttackerName.text + "% hits %3" + getDefenderName.text + "%" + effectiveness,
                    combatInstance.defender);
            }

            // Set the hit entity as the target entity if the attacker is the player
            if(combatInstance.attacker == player) {
                engine.playerController.hud.enemyHealthWindow.SpecifyEnemy(combatInstance.defender);
            }
        }


        /// <summary>
        /// Processes the combat instance as a healing event
        /// </summary>
        private static void ProcessHealingEvent(CombatInstance combatInstance) {
            Engine engine = Engine.instance;
            Entity player = engine.playerController.player;

            // get the names of the actors before they are updated, so it never says you attacked a dead thing
            var getAttackerName = (EGetScreenName)combatInstance.attacker.FireEvent(new EGetScreenName());
            var getDefenderName = (EGetScreenName)combatInstance.defender.FireEvent(new EGetScreenName());

            // check if there was some sort of tool used in the attack, and get its name
            string toolUsed = "";
            if(combatInstance.Get<Entity>("weapon") != null) {
                Entity weapon = combatInstance.Get<Entity>("weapon");
                var getWeaponName = (EGetScreenName)weapon.FireEvent(new EGetScreenName());
                toolUsed = getWeaponName.text;
            }

            // skill name overrides weapon name in message printout
            if(combatInstance.Get<Entity>("skill") != null) {
                Entity skill = combatInstance.Get<Entity>("skill");
                var getSkillName = (EGetScreenName)skill.FireEvent(new EGetScreenName());
                toolUsed = getSkillName.text;
            }

            int healing = combatInstance.Get<int>("healing");

            var healEvent = combatInstance.defender.FireEvent(new EDoHeal() {
                healAmount = healing
            });

            if(toolUsed != "") {
                Engine.LogMessage("%2" + getDefenderName.text + "% is healed for %2" + healing + "% points by %1" + toolUsed,
                    combatInstance.defender);
            }
            else {
                Engine.LogMessage("%2" + getDefenderName.text + "% is healed for %2" + healing + "% points",
                    combatInstance.defender);
            }
        }


        /// <summary>
        /// Calculates the skill modifier between 0.0 and 1.0, based on the users level in the attribute that the skill targets.
        /// If the input user is null, then it will be determined by asking the skill who it is equipped on
        /// </summary>
        public static float GetEntitySkillStrength(Entity user, Entity skill) {
            if(user == null) {
                var getSkillUser = (EGetSkillUser)skill.FireEvent(new EGetSkillUser());
                user = getSkillUser.user;

                if(user == null) {
                    return 1.0f;
                }
            }
            
            var getTargetAttr = (EGetSkillAttribute)skill.FireEvent(new EGetSkillAttribute());

            // get the users attribute level for skills target attribute
            var getAttrLevel = (EGetAttributeLevel)user.FireEvent(new EGetAttributeLevel() {
                target = getTargetAttr.targetAttribute
            });
            
            // for now I guess just say attributes are out of 100? 
            float str = (float)getAttrLevel.level / 100.0f;

            // TODO: some other type of skill modifer level?
            return str;
        }


        /// <summary>
        /// Compares the damage type to the strengths and weaknesses, and modifes the base damage. The effectiveness will be negative if the
        /// damage was reduced, and positive if it was effective. 0 means it did not change. The magnitude indicates how effective / ineffective
        /// </summary>
        public static int CalcDamageResistance(string damageType, int baseDamage, string resistType, out int effectiveness) {
            const string RESIST_TABLE = "Resistances";
            const float RESIST_DMG_RATIO = 0.3f;
            int damage = baseDamage;
            effectiveness = 0;

            if(damageType == null || resistType == null) {
                return damage;
            }

            // Look up the table of damage types and resistances to get strength / weakness, each cell is 1, 0 or -1
            Engine engine = Engine.instance;
            int resistLevel = engine.dataTables.GetValue<int>(resistType, RESIST_TABLE, damageType);

            effectiveness = -resistLevel;

            // calc the damage reduction
            int dmgAdjustement = (int)(effectiveness * baseDamage * RESIST_DMG_RATIO);
            return damage + dmgAdjustement;
        }
        
        /// <summary>
        /// Gets a string representation of the effectiveness of an attack
        /// </summary>
        public static string GetEffectivenessString(int effectiveness, int damageDealt, Entity defender) {
            const float SUPER_EFFECT_THRESHOLD = 0.75f;

            if(damageDealt <= 0) {
                return ", it does nothing";
            }

            var getDefenderHp = (EGetHealth)defender.FireEvent(new EGetHealth());
            if(damageDealt >= (getDefenderHp.maxHealth * SUPER_EFFECT_THRESHOLD)) {
                return ", it's super effective";
            }

            switch(effectiveness) {
                case -1:
                    return ", it's not very effective";
                case 0:
                    return "";
                case 1:
                    return ", it's effective";
            }

            return "";
        }

        /// <summary>
        /// Calculates how far the thrower can throw the thrown entity
        /// </summary>
        public static float GetThrowRange(Entity thrower, Entity thrown) {
            const float MAX_THROW_RANGE = 10.0f;
            const int MAX_THROW_WEIGHT = 200;

            var getStrength = (EGetAttributeLevel)thrower.FireEvent(new EGetAttributeLevel() { target = "strength" });
            int strength = getStrength.level;

            var getWeight = (EGetWeight)thrown.FireEvent(new EGetWeight());
            int thrownWeight = getWeight.weight;

            float maxDistance = 0.0f;
            float strRatio = strength / 100.0f;
            float weightRatio = thrownWeight / MAX_THROW_WEIGHT;
            if(weightRatio > 1.0f) {
                weightRatio = 1.0f;
            }

            // calculate the throw dist by comparing the strength ratio and weight ratio
            Vector2 a = new Vector2(strRatio, weightRatio);
            maxDistance = a.Magnitude() * MAX_THROW_RANGE;

            return maxDistance;
        }



    }
}
