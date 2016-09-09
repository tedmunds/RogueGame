using System;
using libtcod;

namespace RogueGame.Utilities {

    public abstract class ColorConverter {

        /// <summary>
        /// Convert the int to a TCODColor
        /// Assumes the int is in the hex form 0x 00 FF FF FF -> [red, green, blue]
        /// </summary>
        public static TCODColor IntToColor(int color) {
            byte red = (byte)((0x00FF0000 & color) >> 16);
            byte green = (byte)((0x0000FF00 & color) >> 8);
            byte blue = (byte)((0x000000FF & color));
            return new TCODColor(red, green, blue);
        }


        /// <summary>
        /// Interprets the input string as a hex number in the form 0x..... and converts that to an int, and then to color
        /// </summary>
        public static TCODColor HexToColor(string hexColor) {
            System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(Int32));
            Int32 color = (Int32)typeConverter.ConvertFromString(hexColor);

            return IntToColor(color);
        }

        /// <summary>
        /// Converts the input color object into a string representation of the hex value
        /// </summary>
        public static string ColorToHex(TCODColor color) {
            return "#" + color.Red.ToString("X2") + color.Green.ToString("X2") + color.Blue.ToString("X2");
        }
    }
}
