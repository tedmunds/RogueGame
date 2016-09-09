#define CUSTOM_FONT

using System;
using libtcod;

namespace RogueGame {
    
    /// <summary>
    /// Game entry point and libtcod intialization
    /// </summary>
    public class RogueGame {
        private const string GAME_TITLE = "Rogue Game 0.1";
        private const string FONT_SHEET = "custom_lucidal12x12_gs_ro.png";
        public const int WindowWidth = 120;
        public const int WindowHeight = 60;
        public const int keyInitialDelay = 100;
        public const int keyInterval = 50;

        public const bool doFullscreen = false;

        static void Main(string[] args) {
            Console.WriteLine("Rogue Game: By Thomas Edmunds");

#if CUSTOM_FONT
            Console.WriteLine("- Creating custom libTCOD font...");
            TCODConsole.setCustomFont(FONT_SHEET, (int)TCODFontFlags.LayoutAsciiInRow | (int)TCODFontFlags.Greyscale);
#endif

            Console.WriteLine("- Initializing libTCOD console...");
            TCODConsole.initRoot(WindowWidth, WindowHeight, GAME_TITLE, false, TCODRendererType.SDL);
            TCODConsole.setKeyboardRepeat(keyInitialDelay, keyInterval);

            Console.WriteLine("- libTCOD fullscreen = " + doFullscreen.ToString());
            TCODConsole.setFullscreen(doFullscreen);

            Console.WriteLine("- Game Entry point, launching rogue engine...");

            // Init the game engine: game entrypoint
            Engine engine = new Engine(TCODConsole.root);
            int endCode = engine.Run();

            Console.WriteLine("- Game exit with code " + endCode);
            return;
        }
    }
}
