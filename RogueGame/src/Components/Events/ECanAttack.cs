
namespace RogueGame.Components {
    public class ECanAttack : ComponentEvent {
        public Entity asker = null;
        public bool validTarget = false;
    }
}
