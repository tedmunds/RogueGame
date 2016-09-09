using System;
using libtcod;
using RogueGame.Components;

namespace RogueGame.Interface {
    class WindowPlayerState : Window {

        private SkillUserComponent playerSkills;
        private PhysicsComponent playerPhysicsState;
        private AttributesComponent playerAttributes;

        // sub menu for drawing extra info about skill that is highlighted
        private WindowSkillInfo skillInfoWindow;

        public WindowPlayerState(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {            

            skillInfoWindow = new WindowSkillInfo(0, 0, 30, 12, "[skill]", EBorderStyle.ALL, player, parent);
            parent.windows.Add(skillInfoWindow);

            playerSkills = player.Get<SkillUserComponent>();
            playerPhysicsState = player.Get<PhysicsComponent>();
            playerAttributes = player.Get<AttributesComponent>();
        }



        public override void OnDestroy() {
            base.OnDestroy();
            skillInfoWindow.OnDestroy();
        }



        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();

        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();
            const int left = 3;
            const int skillsTopY = 41;
            int width = console.getWidth() - 3;
            Entity targetSkill = null;
            int targetSkillScreenY = 0;
            Vector2 mLoc = ScreenToConsoleCoord(engine.mouseData.CellX, engine.mouseData.CellY);
            
            InitializeConsole();

            // Player statuc area -----------------------------------------------
            DrawText(1, 2, "health: " + playerPhysicsState.health + "/" + playerPhysicsState.maxHealth, Constants.COL_ANGRY);
            DrawText(1, 3, "energy: " + playerSkills.currentEnergy + "/" + playerSkills.maxEnergy, Constants.COL_BLUE);

            // stats
            DrawText(1, 5, "attributes: ", TCODColor.white);

            DrawText(1,  6, "strength  : " + playerAttributes.strength, Constants.COL_BLUE);
            DrawText(1,  7, "wisdom    : " + playerAttributes.wisdom, Constants.COL_BLUE);
            DrawText(1,  8, "agility   : " + playerAttributes.agility, Constants.COL_BLUE);
            DrawText(1,  9, "hammers   : " + playerAttributes.hammers, Constants.COL_BLUE);
            DrawText(1, 10, "polearms  : " + playerAttributes.polearms, Constants.COL_BLUE);
            DrawText(1, 11, "shields   : " + playerAttributes.shields, Constants.COL_BLUE);
            DrawText(1, 12, "throwing  : " + playerAttributes.throwing, Constants.COL_BLUE);
            DrawText(1, 13, "bows      : " + playerAttributes.bows, Constants.COL_BLUE);
            DrawText(1, 14, "crossbows : " + playerAttributes.crossbows, Constants.COL_BLUE);
            

            // Skills area ------------------------------------------------------
            DrawDividerLine(0, skillsTopY - 2, console.getWidth());
            DrawText(1, skillsTopY - 2, " skills ", Constants.COL_NORMAL);
            
            int line = 0;
            foreach(Entity skill in playerSkills.skills) {
                string skillname = skill.Get<SkillComponent>().name;

                int skillY = skillsTopY + line;

                if(mLoc.Y == skillY &&
                    mLoc.X >= left && mLoc.X < skillname.Length + 1 + left) {
                    DrawRect(1, skillY, width, Constants.COL_FRIENDLY);

                    targetSkill = skill;
                    targetSkillScreenY = skillY;

                    // was clicked, activate the skill. The skill components themselves indicate how it is used
                    if(engine.mouseData.LeftButtonPressed) {
                        targetSkill.FireEvent(new EActivateSkill() { activator = player });
                    }
                }

                // if this skill is the pending attack, highlight it differently
                if(skill == playerSkills.pendingAttackSkill) {
                    DrawRect(1, skillY, width, Constants.COL_ANGRY);
                }

                DrawText(1, skillY, (line + 1) + ":", Constants.COL_NORMAL);
                DrawText(left, skillY, skillname, TCODColor.white);

                line += 1;
            }

            // new or different skill is being highlighted, so draw its info
            if(targetSkill != null &&
                (!skillInfoWindow.isVisible || targetSkill != skillInfoWindow.targetSkill)) {
                skillInfoWindow.origin.y = targetSkillScreenY;
                
                skillInfoWindow.origin.x = origin.x + console.getWidth();
                skillInfoWindow.OpenForSkill(targetSkill);
            }
            else if(targetSkill == null && skillInfoWindow.isVisible) {
                skillInfoWindow.Close();
            }

            return console;
        }
    }
}
