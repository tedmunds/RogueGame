using System;


namespace RogueGame.Components {
    public class EApplyForce : ComponentEvent {
        public float force;
        public Vector2 direction;
    }
}
