using libtcod;
using RogueGame.Components;

namespace RogueGame.Interface {
    public class WindowSkillInfo : WindowInspection {

        // the skill whose info is being displayed
        public Entity targetSkill;

        public WindowSkillInfo(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            isVisible = false;
        }

        public void OpenForSkill(Entity skill) {
            OnInspectorOpen();

            targetSkill = skill;

            InitializeConsole();
            SetVisible(true);
        }


        

        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();

            if(targetSkill == null) {
                return;
            }

            SkillComponent skillComp = targetSkill.Get<SkillComponent>();

            // draw skill name
            EGetScreenName getName = (EGetScreenName)targetSkill.FireEvent(new EGetScreenName());
            string screenName = getName.text;

            // draw the description of the skill
            DrawText(2, 1, screenName, Constants.COL_FRIENDLY);

            // common properties of skills: energy
            DrawText(2, 2, "E: " + skillComp.energy, Constants.COL_BLUE);
            PrintMessage(8, 2, "ATTR: %1" + skillComp.attribute, Constants.DEFAULT_COL_SET);

            // print the description as a dynamic text box
            var getSkillDescr = (EGetSkillDescription)targetSkill.FireEvent(new EGetSkillDescription());
            DrawTextBox(2, 4, console.getWidth() - 4, getSkillDescr.description);
        }



        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();
            return console;
        }



    }
}
