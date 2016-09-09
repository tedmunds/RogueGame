using System.Collections.Generic;
using libtcod;

namespace RogueGame.Interface {
    public class WindowLog : Window {
        private const int MAX_LOG_SIZE = 100;
        private const int DISPLAY_SIZE = 8;

        private List<string> log;    
        private string[] display;

        public WindowLog(int x, int y, int w, int h, string title, EBorderStyle borderStyle, Entity player, HUD parent)
            : base(x, y, w, h, title, borderStyle, player, parent) {

            log = new List<string>(MAX_LOG_SIZE);
            display = new string[DISPLAY_SIZE];
            for(int i = 0; i < display.Length; i++) {
                display[i] = "";
            }
        }


        // permament features
        public override void InitializeConsole() {
            base.InitializeConsole();

        }

        // dynamic features
        public override TCODConsole GetUpdatedConsole() {
            base.GetUpdatedConsole();
            InitializeConsole();

            const int left = 2;
            const int top = 1;

            for(int i = 0; i < display.Length; i++) {
                int y = top + (display.Length - i) - 1;
                float fadeOut = (float)i / (float)display.Length;
                fadeOut = 1.0f - (0.3f * fadeOut);

                PrintMessage(left, y, (i + 1) + ": " + display[i], Constants.DEFAULT_COL_SET, fadeOut);
            }

            return console;
        }


        /// <summary>
        /// Prints the input message into the message log area, and stores it in the log. Messages have dynamic 
        /// colouring though the simple markup system outlined in Window::PrintMessage(...)
        /// </summary>
        public void LogMessage(string message, Vector2 sourceLocation) {
            // if the source of the message came from on screen, but was not in LOS of player, dont log it
            if(sourceLocation.X >= 0 && sourceLocation.Y >= 0) {
                if(!engine.world.currentMap.IsInPlayersLOS(sourceLocation)) {
                    return;
                }
            }

            log.Add(message);
            if(log.Count > MAX_LOG_SIZE) {
                log.RemoveAt(log.Count - 1);
            }

            // move all of the displayed messages up in the dispaly
            for(int i = 0; i < display.Length - 1; i++) {
                int row = display.Length - i - 1;
                display[row] = display[row - 1];
            }

            display[0] = message;
        }




    }
}
