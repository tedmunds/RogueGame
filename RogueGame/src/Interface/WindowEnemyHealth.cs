using libtcod;
using RogueGame.Components;

namespace RogueGame.Interface {
    public class WindowEnemyHealth : Window {

        private Entity targetEnemy;


        public WindowEnemyHealth(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            targetEnemy = null;
        }


        public void SpecifyEnemy(Entity enemy) {
            targetEnemy = enemy;
            InitializeConsole();
        }



        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();
            
            if(targetEnemy == null) {
                return;
            }

            var getScreenName = (EGetScreenName)targetEnemy.FireEvent(new EGetScreenName());
            DrawText(4, 0, getScreenName.text, TCODColor.white);

            var getEnemyResist = (EGetResistance)targetEnemy.FireEvent(new EGetResistance());
            PrintMessage(console.getWidth() - 25, 0, "type: %2" + getEnemyResist.resistanceType, Constants.DEFAULT_COL_SET);

            var getEntityHp = (EGetHealth)targetEnemy.FireEvent(new EGetHealth());
            if(getEntityHp.maxHealth > 0) {
                float hpPct = (float)getEntityHp.currentHealth / (float)getEntityHp.maxHealth;
                DrawProgressBar(console.getWidth() / 2 - 6, 0, 13, hpPct, Constants.COL_ANGRY, Constants.COL_BLUE);
            }

        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();

            return console;
        }

    }
}
