
namespace RogueGame.Components {
    /// <summary>
    /// Contains the definitions of the different useage modeds for skills
    /// </summary>
    public abstract class SkillUseMode {
        public enum EType {
            NextAttack,
            Targeted,
            Self,
            //...
        }
    }
}
