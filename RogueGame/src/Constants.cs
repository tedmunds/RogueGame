using System;
using libtcod;

namespace RogueGame {

    // Global name space constants
    public struct CharConstants {
        public const char VERT_LINE = (char)179;
        public const char HORZ_LINE = (char)196;
        public const char NE_LINE = (char)191;
        public const char NW_LINE = (char)218;
        public const char SE_LINE = (char)217;
        public const char SW_LINE = (char)192;

        public const char W_TJOINT = (char)180;
        public const char E_TJOINT = (char)195;
        public const char N_TJOINT = (char)193;
        public const char S_TJOINT = (char)194;
    }

    public abstract class Constants {

        public static TCODColor COL_NORMAL      = new TCODColor(200, 200, 200);
        public static TCODColor COL_ATTENTION   = new TCODColor(232, 228, 125);
        public static TCODColor COL_FRIENDLY    = new TCODColor(128, 224, 103);
        public static TCODColor COL_ANGRY       = new TCODColor(224, 98, 20);
        public static TCODColor COL_BLUE        = new TCODColor(160, 193, 222);
        public static TCODColor COL_DARK_GREEN  = new TCODColor(57, 97, 71);

        /// <summary>
        /// List of all the message colors in specific order that can be referenced by whoever makes messages
        /// </summary>
        public static TCODColor[] DEFAULT_COL_SET = new TCODColor[] {
            COL_NORMAL,
            COL_ATTENTION,
            COL_FRIENDLY,
            COL_ANGRY,
            COL_BLUE,
        };
    }
}
