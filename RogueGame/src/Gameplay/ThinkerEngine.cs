using System;
using RogueGame.Components;
using RogueGame.Map;

namespace RogueGame.Gameplay {
    /// <summary>
    /// Keeps all the AI decision making logic in one place. The main entry point is ProcessThinker(), which returns an action set 
    /// for any given entity and ThinkState
    /// </summary>
    public abstract class ThinkerEngine {
        
        /// <summary>
        /// Process the input state for the input entity. The output action contains all of the 
        /// info on what the input entity should do.
        /// </summary>
        public static ThinkAction ProcessThinker(Entity thinker, ThinkState state) {
            ThinkAction action = new ThinkAction();
            
            if(state.target == null) {
                FindTarget(thinker, state);
            }

            // TODO: other types of moves. This jsut moves to the target
            if(state.target != null) {
                Vector2 toTarget = state.target.position - thinker.position;
                action.moveStep = Vector2.OrthoNormal(toTarget);
            }
            else {
                action.moveStep = new Vector2(0, 0);
            }
            
            return action;
        }



        /// <summary>
        /// Finds a suitable target for the input thinker. Will set the target of the input state
        /// </summary>
        public static void FindTarget(Entity thinker, ThinkState state) {
            // TODO: get target based on entity plan
            Entity player = Engine.instance.world.player;
            if(DetectsTarget(thinker, player)) {
                state.target = player;
            }
        }



        /// <summary>
        /// Returns true if the input thinker is capable of detecting the input target
        /// </summary>
        public static bool DetectsTarget(Entity thinker, Entity target) {
            var getSightRange = (EGetStat_SightRange)thinker.FireEvent(new EGetStat_SightRange());
            float detectRange = getSightRange.range;

            Vector2 toTarget = target.position - thinker.position;
            return (toTarget.Magnitude() <= detectRange);
        }


       

    }
}
