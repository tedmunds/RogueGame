using System.Collections.Generic;
using libtcod;
using RogueGame.Components;

namespace RogueGame.Interface {

    public class WindowItemInfo : Window {

        // the item whose info is being displayed
        public Entity targetItem;        

        public WindowItemInfo(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            isVisible = false;
        }


        public void OpenForItem(Entity item) {
            OnInspectorOpen();

            targetItem = item;

            InitializeConsole();
            SetVisible(true);            
        }


        public void Close() {
            SetVisible(false);
        }


        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();
            
            if(targetItem == null) {
                return;
            }

            EGetScreenName getName = (EGetScreenName)targetItem.FireEvent(new EGetScreenName());
            string screenName = getName.text;

            DrawText(2, 1, screenName, Constants.COL_FRIENDLY);

            var getStatList = (EGetStatList)targetItem.FireEvent(new EGetStatList());
            
            int line = 0;
            foreach(string stat in getStatList.stats) {
                DrawText(2, 2 + line, stat, Constants.COL_NORMAL);

                line++;
            }
        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();


            
            return console;
        }


        

    }
}
