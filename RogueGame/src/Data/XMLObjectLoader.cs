using System;
using System.Xml.Serialization;
using System.IO;

namespace RogueGame.Data {
    public abstract class XMLObjectLoader {
        
        public delegate void PostLoadCallback<T>(T loaded);

        /// <summary>
        /// Loads the xml object found from the project root directory (data folder level), 
        /// And provides an optional callback function delegate that will be called after the XML file is loaded
        /// </summary>
        public static T LoadXMLObject<T>(string file, PostLoadCallback<T> postLoadCallback) where T : class {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            //string filePath = "../../" + file;
            string filePath = file;

            try {
                FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                T loader = serializer.Deserialize(stream) as T;

                Console.WriteLine("Loaded " + typeof(T).Name + " @ " + file);

                if(postLoadCallback != null) {
                    postLoadCallback(loader);
                }
                
                stream.Close();
                return loader;
            }
            catch(IOException e) {
                Console.WriteLine("XMLObjectLoader::LoadXMLObject could find file " + filePath + " :: " + e.Message);
                return null;
            }
        }
    }
}
