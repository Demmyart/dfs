using System;
using System.Data.SQLite;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NamingServer
{
    public class DataBase
    {
        private SQLiteConnection conn = new SQLiteConnection("Data Source=ns.db;Version=3;");

        public DataBase()
        {
            try
            {
                conn.Open();
                SQLiteCommand dbQuery = conn.CreateCommand();
                ExecuteNonQuery("CREATE TABLE IF NOT EXISTS dirs(curr_path TEXT, name TEXT, parent_path TEXT, owner TEXT, PRIMARY KEY(curr_path)); " +
                             "CREATE TABLE IF NOT EXISTS files(full_path TEXT, dir_path TEXT, name TEXT, addr TEXT, reserv_addr TEXT, size TEXT, owner TEXT, PRIMARY KEY(full_path)); " +
                                "CREATE TABLE IF NOT EXISTS storages(id TEXT, ip TEXT, port TEXT, free_space TEXT, PRIMARY KEY(id));" +
                                "CREATE TABLE IF NOT EXISTS file_to_dir(dir_path TEXT, file_path TEXT, PRIMARY KEY(dir_path, file_path));" +
                                "CREATE TABLE IF NOT EXISTS users(name TEXT, PRIMARY KEY(name));" +
                                "CREATE TABLE IF NOT EXISTS logs(id INT, path TEXT, user TEXT, time DATE, action TEXT, PRIMARY KEY(id), FOREIGN KEY(user) REFERENCES users(name));");
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("DB initialization: " + err.Message);
            }
        }

        public void ExecuteNonQuery(string query)
        {
            try
            {
                SQLiteCommand dbQuery = conn.CreateCommand();
                dbQuery.CommandText = query;
                dbQuery.ExecuteNonQuery();
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("ExecuteNonQuery: " + err.Message);
            }
        }

        ///<summary>
        ///Adds new value to the log table, returns <see langword="true"/> with success
        ///</summary>
        public bool Log(string action, string path, string user)
        {
            try
            {
                ExecuteNonQuery(String.Format("INSERT INTO logs (path,user,time,action) VALUES('{0}','{1}',DATETIME('now'),'{2}')", path, user, action));
            }catch{
                return false;
            }
            return true;
        }

        ///<summary>
        ///Return logs of some <see langword="user"/>
        ///</summary>
        public List<string[]> GetLog(string user)
        {
            var logs = new List<string[]>();
            var dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT * from logs WHERE user = '" + user+"'";
            var reader = dbQuery.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new string[] { reader["path"].ToString(), reader["user"].ToString(), reader["time"].ToString(), reader["action"].ToString() });
            }
            reader.Close();
            return logs;
        }

        public List<Directory> GetDirsFromDB(string condition)
        {
            List<Directory> dirs = new List<Directory>();
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT * from dirs WHERE "+condition;
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            Directory dir;
            try
            {
                while (reader.Read())
                {
                    dir = new Directory(reader["name"].ToString(), reader["parent_path"].ToString(), reader["curr_path"].ToString());
                    dir.Owner = reader["owner"].ToString();
                    dirs.Add(dir);
                }
                reader.Close();
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("GetDirsFromDB: "+err.Message);
            }
            return dirs;
        }

        public bool StorageCheck(string ip, string port)
        {
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT * from storages WHERE ip='" + ip + "' AND port='" + port + "'";
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            bool RC;
            if (reader.Read())
            {
                RC=true;
            }
            else 
            {
                RC=false;
            }
            reader.Close();
            return RC;
        }

        public string ChooseMainStorage(string reqSpace)
        {
            string storage="";
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT id FROM storages WHERE free_space +0 > " + reqSpace + " ORDER BY free_space +0 DESC";
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            try
            {
                if (reader.Read())
                {
                    storage = reader["id"].ToString();
                }
                else
                {
                    throw new Exception("No storage servers available.");
                }
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("ChooseMainStorage: " + err.Message);
            }
            reader.Close();
            return storage;
        }

        public string ChooseReplicateStorage(string reqSpace, List<string> idExclude)
        {
            string excludes = "";
            if (idExclude != null)
            {
                for (int i = 0; i < idExclude.Count; ++i)
                {
                    excludes += "'" + idExclude[i] + "', ";
                }
                excludes = excludes.Remove(excludes.Length - 2);
            }
            string storage = "";
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT id FROM storages WHERE id NOT IN (" + excludes + ") AND free_space +0 > " + reqSpace + " ORDER BY free_space +0 DESC";
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            try
            {
                if (reader.Read())
                {
                    storage = reader["id"].ToString();
                }
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("ChooseReplicateStorage: " + err.Message);
            }
            reader.Close();
            return storage;
        }

        public void UpdateStorageFreeSpace(string id, string fileSize)
        {
            Int64 oldSpaceInt=0;
            Int64 fileSizeInt = 0;
            Int64.TryParse(fileSize, out fileSizeInt);
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT free_space FROM storages WHERE id='" + id + "'";
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            if (reader.Read())
            {
                Int64.TryParse(reader["free_space"].ToString(), out oldSpaceInt);
                reader.Close();
                FileSystem.db.ExecuteNonQuery("UPDATE storages SET free_space='" + (oldSpaceInt - fileSizeInt).ToString() + "' WHERE id='" + id + "'");
            }
            else
            {
                throw new Exception("Error in updating storage space");
            }
        }

        public string GetStorageAddressById(string id)
        {
            string storageAddr = "";
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT ip, port FROM storages WHERE id='" + id + "'";
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            try
            {
                if (reader.Read())
                {
                    storageAddr = reader["ip"].ToString() + ":" + reader["port"].ToString();
                }
                reader.Close();
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("GetStorageById: " + err.Message);
            }
            return storageAddr;
        }

        public List<DirFile> GetFilesFromDB(string condition)
        {
            List<DirFile> files = new List<DirFile>();
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT * from files WHERE " + condition;
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            DirFile file;
            try
            {
                while (reader.Read())
                {
                    file = new DirFile(reader["name"].ToString(), reader["addr"].ToString());
                    file.Owner = reader["owner"].ToString();
                    file.Size = reader["size"].ToString();
                    file.ReserveAddress = reader["reserv_addr"].ToString();
                    files.Add(file);
                }
                reader.Close();
            }
            catch (SQLiteException err)
            {
                Console.WriteLine("GetFilesFromDB: " + err.Message);
            }
            return files;
        }

        public bool CheckUserExist(string name)
        {
            SQLiteCommand dbQuery = conn.CreateCommand();
            dbQuery.CommandText = "SELECT * from users WHERE name='" + name +"'";
            SQLiteDataReader reader = dbQuery.ExecuteReader();
            if (reader.Read())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void CloseConnection()
        {
            conn.Dispose();
        }
    }
}
