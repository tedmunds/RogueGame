

namespace RogueGame.Components {
    public class EActivateSkill : ComponentEvent {
        public SkillUseMode.EType useMode;
        public Entity activator = null;
    }
}
