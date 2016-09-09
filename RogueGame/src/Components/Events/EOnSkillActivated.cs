
namespace RogueGame.Components {

    public class EOnSkillActivated : ComponentEvent {
        public Entity skill = null;
        public SkillUseMode.EType useMode;
    }
}
