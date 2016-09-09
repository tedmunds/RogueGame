using System.Collections.Generic;

namespace RogueGame.Components {
    public class EGetStatList : ComponentEvent {
        public List<string> stats = new List<string>(1);
    }
}
