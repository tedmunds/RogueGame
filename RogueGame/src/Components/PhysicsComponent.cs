using System;
using System.Collections.Generic;

namespace RogueGame.Components {

    /// <summary>
    /// Physics component is for anything that can be destroyed or picked up etc.
    /// </summary>
    public class PhysicsComponent : Component {

        public int health;
        public int weight;
        public int size;

        public int maxHealth;

        // does this compoennt block movement
        public bool blocking = true;

        public Vector2 velocity;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            // record what the starting health was
            maxHealth = health;
            velocity = new Vector2(0, 0);

            owner.AddEventResponse(typeof(EDoDamage),       Event_ReceiveDamage);
            owner.AddEventResponse(typeof(EDoHeal),         Event_ReceiveHealth);
            owner.AddEventResponse(typeof(EGetBlockState),  Event_CurrentlyBlocking);
            owner.AddEventResponse(typeof(ESetBlockState),  Event_SetBlocking);
            owner.AddEventResponse(typeof(ECanPickup),      Event_CanPickup);
            owner.AddEventResponse(typeof(EGetSize),        Event_GetSize);
            owner.AddEventResponse(typeof(EGetScreenName),  Event_ScreenName, 0);
            owner.AddEventResponse(typeof(EGetStatList),    Event_AddStatList, 500);
            owner.AddEventResponse(typeof(EGetHealth),      Event_GetHealth, 0);
            owner.AddEventResponse(typeof(EApplyForce),     Event_ApplyForce);
            owner.AddEventResponse(typeof(ENewTurn),        Event_OnNewTurn);
            owner.AddEventResponse(typeof(EGetWeight),      Event_GetWeight);
        }



        public bool Event_ReceiveDamage(ComponentEvent e) {
            EDoDamage damageEvent = (EDoDamage)e;
            Entity dmgInstigator = damageEvent.instigator;
            int baseDamage = damageEvent.damage;

            // Lets other components fill out the resistance value
            var resistEvent = (EResistDamage)owner.FireEvent(new EResistDamage() {
                baseDamage = baseDamage,
                instigator = dmgInstigator
            });

            int reducedDmg = baseDamage - resistEvent.resistance;
            if(reducedDmg < 0) {
                reducedDmg = 0;
            }
            
            // This entity has been destroyed
            health -= reducedDmg;
            if(health <= 0) {
                owner.FireEvent(new EDied() { instigator = dmgInstigator });
                blocking = false;
                health = 0;
            }

            return true;
        }



        public bool Event_ReceiveHealth(ComponentEvent e) {
            EDoHeal healEvent = (EDoHeal)e;
            int amount = healEvent.healAmount;

            // This entity has been destroyed
            health += amount;
            if(health > maxHealth) {
                health = maxHealth;
            }

            return true;
        }




        public bool Event_CurrentlyBlocking(ComponentEvent e) {
            // it is blocking, otherwise leave it ignored
            if(blocking) {
                EGetBlockState blockState = (EGetBlockState)e;
                blockState.blocking = true;
            }

            return true;
        }


        public bool Event_SetBlocking(ComponentEvent e) {
            blocking = ((ESetBlockState)e).blocking;
            return true;
        }


        public bool Event_GetWeight(ComponentEvent e) {
            ((EGetWeight)e).weight = weight;
            return true;
        }



        public bool Event_CanPickup(ComponentEvent e) {
            ECanPickup pickupEvent = (ECanPickup)e;
            Entity asker = pickupEvent.asker;

            // check if this is larger than the asker. If it is, then they cant pick it up
            EGetSize getSize = (EGetSize)asker.FireEvent(new EGetSize());
            int askerSize = getSize.size;

            // the other object is apperently smaller, so this set the payload to false. Also cant pickup self, like ever
            if(size > askerSize || asker == owner) {
                pickupEvent.canPickup = false;
            }
            else {
                pickupEvent.canPickup = true;
            }

            return true;
        }



        public bool Event_GetSize(ComponentEvent e) {
            ((EGetSize)e).size = size;
            return true;
        }



        public bool Event_GetHealth(ComponentEvent e) {
            ((EGetHealth)e).maxHealth = maxHealth;
            ((EGetHealth)e).currentHealth = health;
            return true;
        }


        public bool Event_ScreenName(ComponentEvent e) {
            const string deadName = " remains";

            if(health <= 0 && maxHealth > 0) {
                EGetScreenName screenName = (EGetScreenName)e;
                screenName.text += deadName;
            }
           
            return true;
        }


        public bool Event_AddStatList(ComponentEvent e) {
            List<string> stats = ((EGetStatList)e).stats;
            stats.Add("weight = " + weight);
            return true;
        }

        
        public bool Event_ApplyForce(ComponentEvent e) {
            EApplyForce forceEvent = ((EApplyForce)e);
            
            // set the velocity based on the weight and the force (to find acceleration)
            float acceleration = (float)forceEvent.force / (float)weight;
            velocity += forceEvent.direction* acceleration;
            return true;
        }


        public bool Event_OnNewTurn(ComponentEvent e) {
            // every turn move in the velocity and reduce it by some deceleration
            Vector2 delta = new Vector2(0, 0);
            while(Math.Abs(velocity.X) > 0 || Math.Abs(velocity.Y) > 0) {
                // simplify the direction into cardinal directions
                if(Math.Abs(velocity.X) > Math.Abs(velocity.Y)) {
                    delta.x = Math.Sign(velocity.X);
                }
                else {
                    delta.y = Math.Sign(velocity.Y);
                }

                owner.FireEvent(new EMove() { direction = delta });

                if(delta.Magnitude() >= velocity.Magnitude()) {
                    break;
                }

                velocity -= delta;
            }

            velocity = new Vector2(0, 0);

            return true;
        }



    }
}
