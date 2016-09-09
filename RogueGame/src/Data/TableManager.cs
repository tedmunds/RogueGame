using System;
using System.IO;
using System.Collections.Generic;

namespace RogueGame.Data {

    /// <summary>
    /// Class that holds all of the data tables for the game, in a READ ONLY way.
    /// The data is used to assign values to different object prototypes through a DataMapper for the specific type.
    /// 
    /// Not all tables have data that is compatible with eachother, a given table is unique to itelf, except that they
    /// all have a "name" field as the first column
    /// </summary>
    public class TableManager {

        public const string KEY_FIELD = "name";


        private Engine engine;

        /// <summary>
        /// Set of all tables, accessable by their names
        /// </summary>
        private Dictionary<string, DataTable> tableLibrary;


        public TableManager() {
            engine = Engine.instance;
        }

        /// <summary>
        /// Sets up the table library by loading in the pre-specified list of .data files
        /// </summary>
        public void LoadData(string[] dataFiles) {
            tableLibrary = new Dictionary<string, DataTable>();

            foreach(string filePath in dataFiles) {
                if(filePath == "") {
                    continue;
                }

                DataTable table = DataLoader.LoadParseFile(filePath);
                tableLibrary.Add(table.name, table);
            }
        }


        /// <summary>
        /// Gets teh scpecified table, or null if it doesn't exist
        /// </summary>
        public DataTable GetTable(string tableName) {
            DataTable table;
            if(tableLibrary.TryGetValue(tableName, out table)) {
                return table;
            }

            return null;
        }

        /// <summary>
        /// Will attempt to find the data table which the source table has linked via the linkKey.
        /// Null if no such table has been loaded into the library
        /// </summary>
        public DataTable GetLinkedTable(DataTable sourceTable, string linkKey) {
            string linkedTableName = sourceTable.GetLinkedTableName(linkKey);

            DataTable linkedTable;
            if(tableLibrary.TryGetValue(linkedTableName, out linkedTable)) {
                return linkedTable;
            }

            return null;
        }

        /// <summary>
        /// Attempts to find the value of the foreignDiemnsion in a foreign table that is linked
        /// to the local table by the keyDimension
        /// </summary>
        public T GetForeignValue<T>(string rowKey, DataTable localTable, string keyDimension,
                                    string foreignDimension) {

            DataTable foreignTable = GetLinkedTable(localTable, keyDimension);
            T foreignValue = localTable.GetForeignValue<T>(rowKey, keyDimension, foreignTable, foreignDimension);
            return foreignValue;
        }


        /// <summary>
        /// Gets the value of tge dimension identified by the key in the table, and attempts to cast to the type T
        /// </summary>
        public T GetValue<T>(string rowKey, string tableName, string dimension) {

            DataTable table = GetTable(tableName);
            if(table == null) {
                return default(T);
            }

            T retrieved = table.GetValue<T>(rowKey, dimension);
            return retrieved;
        }


        /// <summary>
        /// Splits the value of the dimension by the ',' character and returns the list of strings
        /// </summary>
        public string[] GetListValue(string rowKey, string tableName, string dimension) {
            char[] dillinators = { ',' };
            string val = GetValue<string>(rowKey, tableName, dimension);

            return val.Split(dillinators, StringSplitOptions.RemoveEmptyEntries);
        }



        /// <summary>
        /// Utility for loading a set of filepaths that point to the data files that should be loaded
        /// </summary>
        public static string[] LoadDataFileList(string listFile) {
            try {
                string[] lines = File.ReadAllLines(listFile);
                return lines;
            }
            catch(IOException e) {
                Console.WriteLine("AdventureEngine::LoadDataFileList failed to open " + listFile + " :: " + e.Message);
                return new string[] { };
            }
        }



    }
}
