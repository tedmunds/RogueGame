using System.Collections.Generic;
using RogueGame.Gameplay;

namespace RogueGame.Components {
    /// <summary>
    /// Basis of the "skill pipeline" where different skill components add effects and do stuff to the entities
    /// </summary>
    public class ECompileSkillEffects : ComponentEvent {
        public Entity user = null;
        public Vector2 baseLocation;
        public float skillStrength = 1.0f;
        public List<CombatInstance> combats = new List<CombatInstance>();
    }
}
