
namespace RogueGame.Components {
    public class EResistDamage : ComponentEvent {
        public int resistance = 0;
        public int baseDamage = 0;
        public Entity instigator = null;
    }
}
