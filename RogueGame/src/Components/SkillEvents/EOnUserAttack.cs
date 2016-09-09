
namespace RogueGame.Components {
    /// <summary>
    /// For when the user of a skill performs an attack that a skill wants to modify
    /// </summary>
    public class EOnUserAttack : ComponentEvent {
        public ECompileAttack attackEvent = null;
        public Entity attacker;
    }
}
