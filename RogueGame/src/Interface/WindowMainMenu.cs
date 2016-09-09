using System;
using System.Collections.Generic;
using libtcod;

namespace RogueGame.Interface {
    public class WindowMainMenu : WindowGameMenu {
        private TCODColor subtleColor = new TCODColor(60, 80, 100);
        
        public WindowMainMenu(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {
            borderStyle = EBorderStyle.NONE;
            buttons = new List<Button>();

            const int buttonWidth = 16;
            int buttonX = x + w / 2 - buttonWidth / 2;

            buttons.Add(new Button(buttonX, 10, buttonWidth, 1, "[START GAME]", 
                elementDark, Constants.COL_FRIENDLY, foregroundColor, 
                StartNewGame));

            buttons.Add(new Button(buttonX, 15, buttonWidth, 1, "[RESUME]",
                elementDark, Constants.COL_FRIENDLY, foregroundColor,
                ResumeGame));

            buttons.Add(new Button(buttonX, 20, buttonWidth, 1, "[SETTINGS]",
                elementDark, Constants.COL_FRIENDLY, foregroundColor,
                GameOptions));
        }

        
        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();
            TCODRandom rng = TCODRandom.getInstance();

            // draw a bunch of random characters in the background for random visual interest
            for(int x = 0; x < console.getWidth(); x++) {
                for(int y = 0; y < console.getHeight(); y++) {
                    char randChar = (char)rng.getInt(0, 255);
                    DrawText(x, y, "" + randChar, subtleColor);
                }
            }

            const string menuTile = " [Rogue Game] ";
            
            // inner box in which the buttons and stuff are drawn
            int innerWidth = 40;
            int innerHeight = 50;
            int innerX = console.getWidth() / 2 - innerWidth / 2;
            int innerY = console.getHeight() / 2 - innerHeight / 2;

            // draw inner box, then main title
            DrawBox(innerX, innerY, innerWidth, innerHeight, backgroundColor, true);
            DrawText(console.getWidth() / 2 - menuTile.Length / 2, innerY, menuTile, foregroundColor);            
        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();

            foreach(Button b in buttons) {
                b.Update(engine.mouseData);
                b.Draw(this);
            }

            return console;
        }


        public void StartNewGame(Button btn) {
            engine.OpenMenu(new WindowCharacterCreateMenu(0, 0, engine.consoleWidth, engine.consoleHeight, null, EBorderStyle.NONE, null, null));
        }


        public void ResumeGame(Button btn) {
            engine.StartNewGame(null);
        }

        public void GameOptions(Button btn) {
            // TODO: options menu
        }

    }
}
