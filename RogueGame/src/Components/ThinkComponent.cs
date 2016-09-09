using System;
using libtcod;
using RogueGame.Map;
using RogueGame.Gameplay;

namespace RogueGame.Components {

    public class ThinkComponent : Component {

        public bool isDead = false;

        public ThinkState thinkerState;

        public override void OnAttach(Entity owner) {
            base.OnAttach(owner);

            thinkerState = new ThinkState();

            owner.AddEventResponse(typeof(ENewTurn), Event_NewTurn);
            owner.AddEventResponse(typeof(EDied), Event_OnDeath);
        }


        public bool Event_NewTurn(ComponentEvent e) {
            if(isDead) {
                return true;
            }

            // Use the thinker engine to get an action for this turn
            ThinkAction turnActions = ThinkerEngine.ProcessThinker(owner, thinkerState);


            // Just wander in a random direction
            //TCODRandom rng = TCODRandom.getInstance();
            //int randDirection = rng.getInt(0, 10);
            //Vector2 moveDirection = new Vector2(0, 0);
            //
            //switch(randDirection) {
            //    case 0:
            //        moveDirection = new Vector2(1, 0);
            //        break;
            //    case 1:
            //        moveDirection = new Vector2(-1, 0);
            //        break;
            //    case 2:
            //        moveDirection = new Vector2(0, 1);
            //        break;
            //    case 3:
            //        moveDirection = new Vector2(0, -1);
            //        break;
            //}

            if(turnActions.moveStep.X != 0 || turnActions.moveStep.Y != 0) {
                owner.FireEvent(new EMove() { direction = turnActions.moveStep });
            }
            
            return true;
        }


        public bool Event_OnDeath(ComponentEvent e) {
            isDead = true;
            return true;
        }


    }
}
