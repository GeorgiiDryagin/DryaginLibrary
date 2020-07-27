using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Drawing;

namespace DryaginLibrary
{
    public class SqliteDataAccess
    {
        public SQLiteConnection myConnection;

        public SqliteDataAccess()
        {
            if (!File.Exists("./dll_table.db"))
                SQLiteConnection.CreateFile("dll_table.db");
            myConnection = new SQLiteConnection("Data Source=dll_table.db");

            if (!CheckTableExists())
                CreateTable();
        }
        public bool CheckTableExists()
        {
            string query = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'dll_ClassesPictures'";
            SQLiteCommand myCommand = new SQLiteCommand(query, myConnection);
            OpenConnection();
            SQLiteDataReader reader = myCommand.ExecuteReader();
            bool res = reader.HasRows;
            CloseConnection();
            return res;
        }
        public void CreateTable()
        {
            string query = @"
            CREATE TABLE 'dll_ClassesPictures'(
                'id'    INTEGER NOT NULL UNIQUE,
                'file_name' TEXT,
                'classes'   TEXT,
                'pictures'  TEXT,
                PRIMARY KEY('id' AUTOINCREMENT)
            )";
            SQLiteCommand myCommand = new SQLiteCommand(query, myConnection);
            OpenConnection();
            myCommand.ExecuteReader();
            CloseConnection();
        }

        public void OpenConnection()
        {
            if (myConnection.State != System.Data.ConnectionState.Open)
            {
                myConnection.Open();
            }
        }

        public void CloseConnection()
        {
            if (myConnection.State != System.Data.ConnectionState.Closed)
            {
                myConnection.Close();
            }
        }

        public void SaveData(DLL_Data Data)
        {
            string query = "insert into dll_ClassesPictures (file_name, classes, pictures) values (@file_name, @classes, @pictures)";
            SQLiteCommand myCommand = new SQLiteCommand(query, myConnection);
            OpenConnection();
            myCommand.Parameters.AddWithValue("@file_name", Data.file_name);
            myCommand.Parameters.AddWithValue("@classes", Data.classes);
            myCommand.Parameters.AddWithValue("@pictures", Data.pictures);
            myCommand.ExecuteNonQuery();
            CloseConnection();
        }

        public List<DLL_Data> LoadData()
        {
            List<DLL_Data> result = new List<DLL_Data>();
            string query = "select * from dll_ClassesPictures";
            SQLiteCommand myCommand = new SQLiteCommand(query, myConnection);
            OpenConnection();
            SQLiteDataReader reader = myCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    result.Add(new DLL_Data()
                    {
                        file_name = reader["file_name"].ToString(),
                        classes = reader["classes"].ToString(),
                        pictures = reader["pictures"].ToString()
                    });
                }
            }
            CloseConnection();
            return result;
        }

        public DataTable LoadTable()
        {
            List<DLL_Data> result = new List<DLL_Data>();
            string query = "select * from dll_ClassesPictures";
            SQLiteCommand myCommand = new SQLiteCommand(query, myConnection);
            OpenConnection();
            SQLiteDataReader dataReader = myCommand.ExecuteReader();

            //reading database
            DataTable dataTable = new DataTable();
            string[] collumns = "file_name,classes".Split(',');
            foreach (string col in collumns)
            {
                dataTable.Columns.Add(col);
            }

            dataTable.Load(dataReader);
            CloseConnection();
            return dataTable;
        }
        public void AnalyzeDll(string path)
        {
            Assembly asm = Assembly.LoadFrom(path);

            string classes_str = "";
            string pictures_str = "";

            Type[] types = asm.GetTypes();
            foreach (Type type in types)
            {
                classes_str += type.Name + " \n";
            }

            string[] names = asm.GetManifestResourceNames();
            foreach (string name in names)
            {
                //if it is a picture
                if (name.EndsWith(".bmp") || name.EndsWith(".jpg") || name.EndsWith(".png"))
                {

                    if (!Directory.Exists(@".\images\"))
                        Directory.CreateDirectory(@".\images\");

                    if (name.EndsWith(".bmp"))
                    {
                        Stream myStream = asm.GetManifestResourceStream(name);
                        Bitmap bmp = new Bitmap(myStream);
                        bmp.Save(@"images\" + asm.GetName() + " " + name);
                    }
                    else
                    {
                        Stream myStream = asm.GetManifestResourceStream(name);
                        Image img = Image.FromStream(myStream);
                        img.Save(@"images\" + asm.GetName() + " " + name);
                    }

                    pictures_str += name + " \n";
                }
            }

            SaveData(new DLL_Data() { file_name = path, classes = classes_str, pictures = pictures_str });
        }
        public void AnalyzeEveryDll()
        {
            var dirs = Directory.GetFiles(@".\", "*.dll");

            foreach (var dir in dirs)
            {
                AnalyzeDll(dir);
            }
        }
    }
}
