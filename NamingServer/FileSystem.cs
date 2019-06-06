using System;
using System.Collections.Generic;

namespace NamingServer
{
    public static class FileSystem
    {
        public static Directory root=new Directory("/", "/", "/");
        public static DataBase db = new DataBase();

        public static void FillDirsFromDB(Directory startDir)
        {
            Directory currDir = startDir;
            List<Directory> Dirs = db.GetDirsFromDB("parent_path='" + currDir.CurrentPath+"'");
            List<DirFile> Files = db.GetFilesFromDB("full_path IN (SELECT file_path FROM file_to_dir WHERE dir_path='"+currDir.CurrentPath+"')");
            for (int i = 0; i < Dirs.Count; ++i)
            {
                currDir.Directories.Add(Dirs[i].Name, Dirs[i]);
                FillDirsFromDB(Dirs[i]);
            }
            for (int i = 0; i < Files.Count; ++i)
            {
                currDir.Files.Add(Files[i].Name, Files[i]);
            }
        }

        public static Directory GetDirectory(string path)
        {
            if (path[0]!='/')
            {
                throw new Exception("No such directory");
            }
            if (path == "/")
            {
                return root;
            }
            else
            {
                string[] dirs = path.Split('/');
                Directory endDir = root;
                for (int i = 1; i < dirs.Length; ++i)
                {
                    if (endDir.Directories.ContainsKey(dirs[i]))
                    {
                        endDir = endDir.Directories[dirs[i]];
                    }
                    else
                    {
                        throw new Exception("No such directory");
                    }
                }
                return endDir;
            }
        }

        public static string GetFullPathFromUser(string userName, string path)
        {
            if (path == "/")
            {
                return "/" + userName;
            }
            else
            {
                return "/" + userName + path;
            }
        }

        public static string GetUserPathFromFull(string userName, string path)
        {
            if (path == "/" + userName || path=="/")
            {
                return "/";
            }
            else
            {
                return path.Remove(0, userName.Length + 1);
            }
        }
    }

    public class Directory
    {
        public string Name { get; set; }
        public string ParentPath { get; set; }
        public string CurrentPath { get; set; }
        public string Owner { get; set; }
        public Dictionary<string, Directory> Directories = new Dictionary<string, Directory>();
        public Dictionary<string, DirFile> Files = new Dictionary<string, DirFile>();

        public Directory(string name, string parentPath, string currentPath)
        {
            Name = name;
            ParentPath = parentPath;
            CurrentPath = currentPath;
        }

        public void CreateSubDir(string insertedName, string ownerName)
        {
            if (Directories.ContainsKey(insertedName))
            {
                throw new Exception("Directory already exist");
            }
            if (insertedName=="" || insertedName=="/")
            {
                throw new Exception("Can not create directory with specified name");
            }
            Directory insertedDir;
            if (Name == "/")
            {
                insertedDir = new Directory(insertedName, Name, Name+insertedName);
            }
            else
            {
                if (ParentPath == "/")
                {
                    insertedDir = new Directory(insertedName, ParentPath+Name, ParentPath+Name+"/"+insertedName);
                }
                else
                {
                    insertedDir = new Directory(insertedName, ParentPath + "/" + Name, ParentPath + "/" + Name+"/"+insertedName);
                }
            }
            insertedDir.Owner = ownerName;
            Directories.Add(insertedName, insertedDir);
            FileSystem.db.ExecuteNonQuery("INSERT INTO dirs(curr_path, name, parent_path, owner) VALUES ('"+insertedDir.CurrentPath+"', '"+insertedDir.Name+"', '"+insertedDir.ParentPath+"', '"+ownerName+"')");
        }

        public void DeleteSubDir(string deletedName)
        {
            if (!Directories.ContainsKey(deletedName))
            {
                throw new Exception("No such directory");
            }
            Directory deletedDir = Directories[deletedName];
            List<string> dirStorages = GetDirStorages(deletedDir);
            for (int i = 0; i < dirStorages.Count; ++i)
            {
                int response = StorageAPI.GetRequest(dirStorages[i], "/api/name/deletedir?path=" + FileSystem.GetUserPathFromFull(deletedDir.Owner, deletedDir.CurrentPath)+"&user="+deletedDir.Owner);
                if (!(response == 200 || response == 404))
                {
                    throw new Exception("Error in removing directory");
                }
            }
            FileSystem.db.ExecuteNonQuery("DELETE FROM files WHERE dir_path='"+deletedDir.CurrentPath+"'");
            FileSystem.db.ExecuteNonQuery("DELETE FROM file_to_dir WHERE dir_path='"+deletedDir.CurrentPath+"'");
            FileSystem.db.ExecuteNonQuery("DELETE FROM dirs WHERE curr_path='" + deletedDir.CurrentPath + "'");
            Directories.Remove(deletedName);
        }

        public void RegFile(string name, string size, string storageId, string owner)
        {
            string mainAddress = FileSystem.db.GetStorageAddressById(storageId);
            string filePath;
            if (CurrentPath == "/")
            {
                filePath = CurrentPath + name;
            }
            else
            {
                filePath = CurrentPath + "/" + name;
            }
            List<string> excludedId = new List<string>();
            excludedId.Add(storageId);
            int response = 0;
            bool foundReserve = false;
            string reserveId="";
            string reserveAddress="";
            do
            {
                reserveId = FileSystem.db.ChooseReplicateStorage(size, excludedId);
                if (reserveId != "")
                {
                    reserveAddress = FileSystem.db.GetStorageAddressById(reserveId);
                    response = StorageAPI.GetRequest(reserveAddress, "api/name/replicate?path=" + FileSystem.GetUserPathFromFull(owner, CurrentPath) + "&name=" + name + "&target=" + mainAddress+"&user="+owner);
                    if (response == 200)
                    {
                        foundReserve = true;
                        break;
                    }
                    else
                    {
                        excludedId.Add(reserveId);
                    }
                }
            } while (reserveId != "") ;
            DirFile file = new DirFile(name, mainAddress);
            file.Size = size;
            file.Owner = owner;
            Files.Add(name, file);
            if (foundReserve)
            {
                FileSystem.db.ExecuteNonQuery("INSERT INTO files(full_path, dir_path, name, addr, size, reserv_addr, owner) VALUES ('" + filePath + "', '" + CurrentPath + "', '" + name + "', '" + mainAddress + "', '" + size + "', '" + reserveAddress + "', '"+owner+"')");
                Files[name].ReserveAddress = reserveAddress;
                FileSystem.db.UpdateStorageFreeSpace(reserveId, size);
            }
            else
            {
                FileSystem.db.ExecuteNonQuery("INSERT INTO files(full_path, dir_path, name, addr, size, owner) VALUES ('" + filePath + "', '" + CurrentPath + "', '" + name + "', '" + mainAddress + "', '" + size + "', '"+owner+"')");
            }
            FileSystem.db.ExecuteNonQuery("INSERT INTO file_to_dir(dir_path, file_path) VALUES ('" + CurrentPath + "', '" + filePath + "')");
        }

        public List<string> GetDirStorages(Directory directory)
        {
            List<string> storages = new List<string>();
            foreach (KeyValuePair<string, DirFile> file in directory.Files)
            {
                if (!storages.Contains(file.Value.Address))
                {
                    storages.Add(file.Value.Address);
                }
                if (file.Value.ReserveAddress!=null && !storages.Contains(file.Value.ReserveAddress))
                {
                    storages.Add(file.Value.ReserveAddress);
                }
            }
            return storages;
        }

        public void DeleteFile(string fileName)
        {
            if (!Files.ContainsKey(fileName))
            {
                throw new Exception("No such file");
            }
            string full_path;
            if (CurrentPath=="/")
            {
                full_path = CurrentPath + fileName;
            }
            else
            {
                full_path = CurrentPath + "/" + fileName;
            }
            string owner = Files[fileName].Owner;
            int response = StorageAPI.GetRequest(Files[fileName].Address, "api/name/delete?path=" + FileSystem.GetUserPathFromFull(owner, full_path)+"&user="+owner);
            if (!(response == 200 || response == 404))
            {
                throw new Exception("Error in removing file");
            }
            if (Files[fileName].ReserveAddress!=null)
            {
                response = StorageAPI.GetRequest(Files[fileName].ReserveAddress, "api/name/delete?path=" + FileSystem.GetUserPathFromFull(owner, full_path) + "&user=" + owner);
                if (!(response == 200 || response == 404))
                {
                    throw new Exception("Error in removing file");
                }
            }
            FileSystem.db.ExecuteNonQuery("DELETE FROM files WHERE full_path='"+full_path+"'");
            FileSystem.db.ExecuteNonQuery("DELETE FROM file_to_dir WHERE file_path='" + full_path + "'");
            Files.Remove(fileName);
        }
    }

    public class DirFile
    {
        public DirFile(string name, string address)
        {
            Name = name;
            Address = address;
        }
        public string Name { get; set; }
        public string Address { get; set; }
        public string ReserveAddress { get; set; }
        public string Size { get; set; }
        public string Owner { get; set; }
    }
}
