namespace RogueGame.Components {
    public class EGetBlockState : ComponentEvent {
        public bool blocking = false;
        public Entity asker = null;
    }
}
