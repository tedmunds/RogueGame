using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;

namespace RogueGame.Data {
    
    /// <summary>
    /// The base Object that gets deserialzed from the file
    /// </summary>
    [XmlRoot("blueprintSet")]
    public class BlueprintSet {

        [XmlArray("blueprints")]
        [XmlArrayItem("object")]
        public EntityBlueprint[] blueprints;
    }

    /// <summary>
    /// Represents the data for a single entity with any components (parts)
    /// </summary>
    public class EntityBlueprint {
        [XmlAttribute("parent")]
        public string parent;

        [XmlAttribute("name")]
        public string name;

        [XmlElement("part")]
        public List<BlueprintPart> parts;
    }

    /// <summary>
    /// Represents the data for a component
    /// </summary>
    public class BlueprintPart {
        [XmlAttribute("type")]
        public string type;

        [XmlAnyAttribute]
        public XmlAttribute[] parameters;
    }
}
