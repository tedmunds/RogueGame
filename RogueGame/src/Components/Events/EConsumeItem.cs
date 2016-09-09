
namespace RogueGame.Components {
    public class EConsumeItem : ComponentEvent {
        public string itemName = "";
        public bool hasItem = false;
        public Entity consumedItem = null;
    }
}
