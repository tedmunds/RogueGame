
namespace RogueGame.Components {

    public class EDoDamage : ComponentEvent {
        public int damage = 0;
        public string damageType = "";
        public Entity instigator = null;
        public int effectiveness = 0;
    }
}
