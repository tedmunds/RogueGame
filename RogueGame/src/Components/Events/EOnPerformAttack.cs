using System;


namespace RogueGame.Components {
    /// <summary>
    /// Asks the rest of the components if the supplied damage event is okay to use
    /// </summary>
    public class EOnPerformAttack : ComponentEvent {
        public ECompileAttack damageEvent;
        public bool canPerform = false;
    }
}
