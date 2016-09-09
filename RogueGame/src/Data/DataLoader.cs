//#define VERBOSE

using System;
using System.Collections.Generic;
using System.IO;

namespace RogueGame.Data {

    public class DataTable {
        public string name;

        /// <summary>
        /// The header links the dimension names, to the index of that dimension in the tables data lists
        /// </summary>
        private Dictionary<string, int> header;

        /// <summary>
        /// The data is a big list of arrays, which are each identified by a key string
        /// </summary>
        private Dictionary<string, string[]> data;

        /// <summary>
        /// Links specific dimensions (key) to a seperate dataTable (value), so that the values of the dimension can be
        /// used to get an row from the target table
        /// </summary>
        private Dictionary<string, string> links;

        public DataTable(string name) {
            this.name = name;
            header = new Dictionary<string, int>();
            data = new Dictionary<string, string[]>();
            links = new Dictionary<string, string>();
        }

        public void SetHeader(string[] dimensions) {
            for(int i = 0; i < dimensions.Length; i++) {
                header.Add(dimensions[i], i);
            }
        }

        public void AddTableLink(string dimension, string linkTable) {
            links.Add(dimension, linkTable);
        }

        public void AddDataRow(string[] dataRow) {
            string rowKey = dataRow[0];
            data.Add(rowKey, dataRow);
        }


        /// <summary>
        /// Checks if there is an entry in this table with the input key
        /// </summary>
        public bool HasEntry(string entryKey) {
            const string testDim = "name";
            string key = GetValue<string>(entryKey, testDim);
            return (key != null);
        }

        /// <summary>
        /// Returns the number of data points in this table
        /// </summary>
        public int NumData() {
            return data.Count;
        }

        /// <summary>
        /// Gets a value fromt he data table: rowkey identifies which instance of the data you want, and and dimension indicates 
        /// which field of that instance you want. The values will be converted to type T on return, since they are stored as strings.
        /// </summary>
        public T GetValue<T>(string rowKey, string dimension) {
            int dimIdx;

            // Find the column index of the specified dimension
            if(header.TryGetValue(dimension, out dimIdx)) {
                string[] dataRow;

                // Grab the row of data whose key mathces the input row key (the name of the data row)
                if(data.TryGetValue(rowKey, out dataRow)) {
                    // And return the data at the dimension index grabbed earlier
                    string val = dataRow[dimIdx];

                    System.ComponentModel.TypeConverter converter;

                    // There is a special case for converting characters from integers: assume if the length is 1 then its just a char
                    if(typeof(T) == typeof(char)) {
                        if(val.Length == 1) {
                            converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(char));
                            return (T)converter.ConvertFromString(val);
                        }
                        else {
                            converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(int));
                            object tempAsInt = converter.ConvertFromString(val);
                            int intVal = (int)tempAsInt;
                            object charVal = (char)intVal;
                            return (T)charVal;
                        }
                    }

                    converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(T));
                    return (T)converter.ConvertFromString(val);
                }
                else {
                    Console.WriteLine("DataTable::GetValue " + name + " does not have an entry named <" + rowKey + ">");
                }
            }
            else {
                Console.WriteLine("DataTable::GetValue " + name + " does not have dimension <" + dimension + ">");
            }

            return default(T);
        }

        /// <summary>
        /// Returns a list of all of the row identifiers
        /// </summary>
        public Dictionary<string, string[]>.KeyCollection GetRowKeys() {
            return data.Keys;
        }

        /// <summary>
        /// <para>Gets a value from a different table, by using a value from this table as a foreign key.
        /// rowKey indicates the row in this table to use, keyDim indicates the dimension in this table to use as the key.
        /// The resulting value is used as a rowKey in foreignTable, and foreignDimension indicates which dimension
        /// of that data row to return</para>
        /// 
        /// <para>Ex. string description = MonsterTable.GetForeignValue("Scorpion", "desc", DescriptionTable, "long_desc");</para>
        /// <para>--> Will return the long_desc value  from the Description table corresponding to the value in the Scorpion "desc" field in the Monser table</para>
        /// </summary>
        public T GetForeignValue<T>(string rowKey, string keyDim,
                                    DataTable foreignTable, string foreignDimension) {
            string foreignKey = GetValue<string>(rowKey, keyDim);

            T val = foreignTable.GetValue<T>(foreignKey, foreignDimension);
            return val;
        }

        /// <summary>
        /// Gets the name of the table that this table has linked via the input key.
        /// will return empty string if the input key is not linked to any table
        /// </summary>
        public string GetLinkedTableName(string linkKey) {
            string linkedTableName;
            if(links.TryGetValue(linkKey, out linkedTableName)) {
                return linkedTableName;
            }

            return "";
        }

    }



    /// <summary>
    /// Loads .data files, and parses it into a dataTable object
    /// 
    /// Data files are tables where the first column is always the "name" dimension that can be used to link rows b/w tables
    /// Each table has a set of "table links" that designate a table dimension to be used as a foriegn key to another table
    /// row
    /// </summary>
    public class DataLoader {

        private const string COMMENT_CHAR = "#";
        private const string LINK_CHAR = ">";

        private static char[] WORD_SEPERATORS = { '\t' };

        public static DataTable LoadParseFile(string fileName) {
            //string filePath = "../../" + fileName;
            string filePath = fileName;

            try {
                string[] lines = System.IO.File.ReadAllLines(filePath);

                // The row of the table section of the data that it reading
                //int tableRow = 0;

                // The row of valid data within the file that it is reading (exluces empty or comment lines)
                bool hasReadHeader = false;
                int lineNum = -1;

                string parserType = "";

                List<string> linkColumns = new List<string>();
                List<string> linkTargets = new List<string>();

                // Begin scanning the file
                foreach(string line in lines) {
                    lineNum += 1;

                    if(line.Length == 0) {
                        continue;
                    }

                    // Allow for lines with comments, just skip over them
                    if(line.StartsWith(COMMENT_CHAR)) {
                        continue;
                    }

                    if(!hasReadHeader) {
                        parserType = line;
                        hasReadHeader = true;
#if VERBOSE
                        Console.WriteLine("DataLoader::LoadParseFile parsing " + fileName + " with the <" + parserType + "> parser");
#endif
                        continue;
                    }

                    // Seperate words out by spaces / tabs
                    string[] words = line.Split(WORD_SEPERATORS, StringSplitOptions.RemoveEmptyEntries);

                    // Check for a link line
                    if(line.StartsWith(LINK_CHAR)) {
                        string targetCol = words[1];
                        string linkFile = words[2];

                        linkColumns.Add(targetCol);
                        linkTargets.Add(linkFile);
#if VERBOSE
                        Console.WriteLine("DataLoader::LoadParseFile " + fileName + " links <" + targetCol + "> to <" + linkFile + ">");
#endif
                        continue;
                    }

                    DataTable dataTable = ParseTable(lines, lineNum, parserType);

                    for(int i = 0; i < linkColumns.Count; i++) {
                        dataTable.AddTableLink(linkColumns[i], linkTargets[i]);
                    }

                    Console.WriteLine("DataLoader::LoadParseFile loaded table " + parserType + " from " + fileName);
                    return dataTable;
                }
            }
            catch(IOException e) {
                Console.WriteLine("DataLoader::LoadParseFile failed to open " + filePath + " :: " + e.Message);
            }

            return null;
        }


        private static DataTable ParseTable(string[] data, int headerIdx, string tableName) {
            string[] dimensions = data[headerIdx].Split(WORD_SEPERATORS, StringSplitOptions.RemoveEmptyEntries);

            DataTable dataTable = new DataTable(tableName);
            dataTable.SetHeader(dimensions);

            for(int i = headerIdx + 1; i < data.Length; i++) {
                string[] values = data[i].Split(WORD_SEPERATORS, StringSplitOptions.RemoveEmptyEntries);

                if(values.Length == 0) {
                    continue;
                }

                dataTable.AddDataRow(values);
            }

#if VERBOSE
            foreach(string dim in dimensions) {
                Console.Write(dim + " : ");
                foreach(string name in dataTable.GetRowKeys()) {
                    Console.Write(dataTable.GetValue<string>(name, dim) + ",\t");
                }
                Console.WriteLine("");
            }
#endif
            return dataTable;
        }



    }
}
