namespace RogueGame.Components {
    /// <summary>
    /// Fired when a skill becomes equipped on the user
    /// </summary>
    public class ESkillEquipped : ComponentEvent {
        public Entity user = null;
    }
}
