using System.Collections.Generic;

namespace RogueGame.Components {
    public class ETargetSkill : ComponentEvent {
        public Vector2 location = new Vector2(0, 0);
        public List<Entity> targets = new List<Entity>();
    }
}
