using System;
using libtcod;
using RogueGame.Components;
using RogueGame.Map;

namespace RogueGame.Interface {

    public class WindowInspection : Window {

        public AreaMap.Tile inspectTile;
        public Vector2 currentLocation;

        protected const int left = 2;
        protected const int top = 1;

        public WindowInspection(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            isVisible = false;
        }
        
        public void OpenForTile(AreaMap.Tile tile, Vector2 location) {
            OnInspectorOpen();
            inspectTile = tile;
            currentLocation = location;
            InitializeConsole();


            SetVisible(true);
        }

        public void Close() {
            SetVisible(false);
        }

        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();
                   

            if(inspectTile.entities == null) {
                return;
            }

            // setup the info based on the entities on the current tile
            int line = 0;
            foreach(Entity entity in inspectTile.entities) {
                EGetScreenName getName = (EGetScreenName)entity.FireEvent(new EGetScreenName());

                DrawText(left, top + line, getName.text, Constants.COL_NORMAL);

                // draw some other info about the entity
                var getEntityHp = (EGetHealth)entity.FireEvent(new EGetHealth());
                if(getEntityHp.maxHealth > 0) {
                    float hpPct = (float)getEntityHp.currentHealth / (float)getEntityHp.maxHealth;
                    DrawProgressBar(console.getWidth() - 10, top + line, 8, hpPct, Constants.COL_ANGRY, Constants.COL_BLUE);
                }

                line += 1;
            }
        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();

            return console;
        }



    }
}
