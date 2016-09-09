using System;
using RogueGame.Gameplay;
using RogueGame.Map;

namespace RogueGame.Components {
    public class AttackComponent : Component {

        public int strength;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);
            owner.AddEventResponse(typeof(EDoAttack), Event_OnAttack, 0);
            owner.AddEventResponse(typeof(EThrowItem), Event_ThrowItem, 0);
        }


        public bool Event_OnAttack(ComponentEvent e) {
            Entity target = ((EDoAttack)e).target;

            // check if the attack has the range to even perfom this attack
            var getRangeEvent = (EGetAttackRange)owner.FireEvent(new EGetAttackRange());
            int dist = Vector2.TaxiDistance(target.position, owner.position);
            if(dist > 2 && dist > getRangeEvent.range) {
                return true;
            }

            // otherwise, calculate the damage to be dealt, then send off the damage event
            int baseDamage = strength;

            // fire off another event that will gather info from other components about the specifics of the attack
            var attackEvent = (ECompileAttack)owner.FireEvent(new ECompileAttack() {
                combat = new CombatInstance(owner, target) {
                    { "damage", baseDamage}
                }
            });

            // notify other components about the attack
            var checkAttackEvent = (EOnPerformAttack)owner.FireEvent(new EOnPerformAttack() {
                damageEvent = attackEvent,
                canPerform = true
            });

            // back out before applying the damage, in case some other component doesn't want the attack to be performed
            if(!checkAttackEvent.canPerform) {
                return true;
            }

            CombatEngine.ProcessCombat(attackEvent.combat);
            return true;
        }



        public bool Event_ThrowItem(ComponentEvent e) {            
            AreaMap map = Engine.instance.world.currentMap;
            var throwEvent = ((EThrowItem)e);
            string itemType = throwEvent.itemName;

            // try to find the item on the entity to throw it
            var consumeItemEvent = (EConsumeItem)owner.FireEvent(new EConsumeItem() { itemName = itemType });
            if(!consumeItemEvent.hasItem) {
                return true;
            }

            // This enttiy did have the item to throw
            Entity projectile = consumeItemEvent.consumedItem;

            // Get the thrwoer strength and item weight, in order to calc both damage and max throw distance
            var getStrength = (EGetAttributeLevel)owner.FireEvent(new EGetAttributeLevel() { target = "strength" });
            int strength = getStrength.level;
            float strRatio = (strength / 100.0f);

            var getWeight = (EGetWeight)owner.FireEvent(new EGetWeight());
            int thrownWeight = getWeight.weight;

            float maxDistance = CombatEngine.GetThrowRange(owner, projectile);
            int throwDamage = (int)(strRatio * thrownWeight);

            // if the target position is farther than the max distance, select the nearest point in that direction
            Vector2 targetLoc = throwEvent.targetLocation;
            Vector2 toTarget = targetLoc - owner.position;
            if(toTarget.Magnitude() > maxDistance) {
                Vector2 dir = toTarget.Normalized();
                targetLoc = owner.position + (dir * maxDistance);
            }

            // check where the item hits, if there is something in the way
            Vector2 hitNormal;
            Vector2 endPosition = map.CollisionPointFromTo(owner.position, targetLoc, out hitNormal);
            
            // find the target to hit, and apply some damage and knock them back
            Entity[] targets = map.GetAllObjectsAt(endPosition.X, endPosition.Y);
            foreach(Entity hit in targets) {
                CombatInstance combat = new CombatInstance(owner, hit) {
                    { "damage", throwDamage},
                    { "weapon", projectile}
                };

                CombatEngine.ProcessCombat(combat);
            }

            // the thrown item ends up next to the target location, or the nearest valid location
            toTarget = Vector2.OrthoNormal(toTarget);
            endPosition = endPosition - toTarget;
            endPosition = map.GetNearestValidMove(endPosition);
            Engine.instance.world.SpawnExisting(projectile, endPosition);

            return true;
        }


    }
}
