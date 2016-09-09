

namespace RogueGame.Gameplay {
    /// <summary>
    /// The state of a thinker entity. The thinker engine uses this to determine what actions the entity should take when processed.
    /// This state should only be updated in the ThinkerEngine
    /// </summary>
    public class ThinkState {
        public Entity target = null;
    }
}
