using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace RogueGame.Data {
    /// <summary>
    /// Utility to save and load a complete game save. A save file contains sub sections for the current map, seeds for previous maps for backtracking, 
    /// and the player state. 
    /// </summary>
    public abstract class GameSave {

        /// <summary>
        /// Creates a complete save file for the given world state, and uses the supplied name.
        /// Returns true if the save was created. If anything goes wrong, an error will be logged and it will return false.
        /// </summary>
        public static bool SaveGamestate(World gamestate, string saveName) {
            IFormatter formatter = new BinaryFormatter();

            try {
                FileStream stream = new FileStream(saveName, FileMode.Create);
                formatter.Serialize(stream, gamestate);
                stream.Close();
            }
            catch(IOException e) {
                Console.WriteLine("ERROR: GameSave::SaveGamestate - " + e.Message);
                return false;
            }

            return true;
        }

        public static bool LoadGameSave(Engine engine, ref World gamestate, string saveName) {
            IFormatter formatter = new BinaryFormatter();

            try {
                FileStream stream = new FileStream(saveName, FileMode.Open);
                
                // Delete the old gamestate first
                gamestate.PreDeserializeCleanup();

                gamestate = (World)formatter.Deserialize(stream);
                gamestate.PostDeserializeInit();
                engine.playerController.ReInitController(gamestate.player);

                stream.Close();
            }
            catch(IOException e) {
                Console.WriteLine("ERROR: GameSave::LoadGameSave - " + e.Message);
                return false;
            }

            return true;
        }

    }
}
