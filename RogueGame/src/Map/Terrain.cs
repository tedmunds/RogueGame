using System;
using System.Xml.Serialization;
using libtcod;
using RogueGame.Utilities;

namespace RogueGame.Map {

    /// <summary>
    /// Little container for info about a given class of terrain:
    /// A single instance of terrain is shared among all tiles of that type, it is a data container
    /// </summary>
    public class Terrain {
        public enum EMobilityLevel {
            Passable,
            BlocksMove,
            BlocksVisilbity
        }

        [XmlElement("mobility")]
        public EMobilityLevel mobility;

        [XmlAttribute("name")]
        public string name;

        [XmlElement("character")]
        public int ch;
        public char Ch {
            get { return (char)ch; }
        }

        [XmlElement("foreground")]
        public string foregroundHex;

        [XmlElement("background")]
        public string backgroundHex;
        
        [XmlIgnore]
        public TCODColor bg;

        [XmlIgnore]
        public TCODColor fg;

        public Terrain() {

        }

        public Terrain(string name, EMobilityLevel mobility, TCODColor bg, TCODColor fg, char ch) {
            this.name = name;
            this.mobility = mobility;
            this.bg = bg;
            this.fg = fg;
            this.ch = ch;
        }


        public void PostLoadInitialization() {
            bg = ColorConverter.HexToColor(backgroundHex);
            fg = ColorConverter.HexToColor(foregroundHex);
        }
    }
}
