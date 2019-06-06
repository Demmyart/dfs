using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StorageServer
{
    public static class FileSystem
    {
        public static string StorageID { get; private set; }

        public static string NameServerAddress { get; private set; }

        public static string StoragePath { get; private set; }

        private static string storageRoot;

        public static bool Initialized { get; private set; }

        public static long MaxSize { get; private set; }

        public static string Port { get; private set; }

        public static long FreeSpace { get; set; }

        public static void Init(){
            if (Initialized){
                return;
            }

            if (!parseConfig()){
                throw new HttpResponseException(System.Net.HttpStatusCode.InternalServerError);
            }
            FreeSpace = MaxSize;

            //NameServerAddress = Environment.GetEnvironmentVariable("NAMING_ADDRESS");

            //NameServerAddress = "172.17.0.2:33033";

            var pid = Process.GetCurrentProcess().Id.ToString();


            try {
                Port = File.OpenText("Port").ReadLine();
            }
            catch{
                Console.WriteLine("Port file wasn't fine");
                return;
            }


            string id = "";
           
            Random rng = new Random();
            if (File.Exists("IDAtNameServer"))
            {
                StreamReader reader = new StreamReader(File.Open("IDAtNameServer", FileMode.Open));
                id = reader.ReadLine();
                reader.Close();

                long size = 0;
                if(Directory.Exists(storageRoot + id + '/'))size = DirSize(new DirectoryInfo(storageRoot + id + '/'));
                FreeSpace -= size;

                var response = Api.Get(NameServerAddress, String.Format("/storageConn?id={0}&port={1}&free_space={2}", id, Port, FreeSpace.ToString()));

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    StoragePath = storageRoot + id + '/';
                    StorageID = id;
                    Initialized = true;
                }
                else {
                    id = rng.Next().ToString();
                    response = Api.Get(NameServerAddress, String.Format("/storageReg?id={0}&port={1}&free_space={2}", id, Port, MaxSize.ToString()));

                    while (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                    {
                        id = rng.Next().ToString();
                        response = Api.Get(NameServerAddress, String.Format("/storageReg?id={0}&port={1}&free_space={2}", id, Port, MaxSize.ToString()));
                    }


                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var writer = new StreamWriter(File.Open("IDAtNameServer", FileMode.Create));
                        writer.WriteLine(id);
                        writer.Flush();
                        writer.BaseStream.Close();
                        writer.Close();
                        StoragePath = storageRoot + id + '/';
                        StorageID = id;
                        Initialized = true;
                    }
                    else
                    {
                        throw new HttpResponseException(System.Net.HttpStatusCode.InternalServerError);
                    }
                }
            } 
            else {
                id = rng.Next().ToString();

                var response = Api.Get(NameServerAddress, String.Format("/storageReg?id={0}&port={1}&free_space={2}", id, Port, MaxSize.ToString()));

                for (int i = 0; i < 20 && response.StatusCode == System.Net.HttpStatusCode.Conflict;i++)
                {
                    id = rng.Next().ToString();
                    response = Api.Get(NameServerAddress, String.Format("/storageReg?id={0}&port={1}&free_space={2}", id, Port, MaxSize.ToString()));
                }

                if( response.StatusCode == System.Net.HttpStatusCode.InternalServerError){
                    Console.WriteLine("Storage with this IP:PORT already registered");
                    throw new HttpResponseException(System.Net.HttpStatusCode.InternalServerError);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var writer = new StreamWriter(File.Open("IDAtNameServer", FileMode.Create));
                    writer.WriteLine(id);
                    writer.Flush();
                    writer.Close();
                    StoragePath = storageRoot + id + '/';
                    StorageID = id;
                    Initialized = true;
                }
                else
                {
                    throw new HttpResponseException(System.Net.HttpStatusCode.InternalServerError);
                }
            }
            CreateDirectoryRecursively(StoragePath);
        }

        public static FileStream GetFileStream(string path){
            return File.Open(StoragePath + path, FileMode.Open,FileAccess.Read,FileShare.Read);
        }


        public static void Delete(string path)
        {
            File.Delete(StoragePath + path);
            File.Delete(StoragePath + path + ".meta");
        }

        public static bool DeleteDir(string path){
            bool result;
            var combined = (StoragePath + path + "/").Replace("//","/");

            try
            {
                Directory.Delete(combined, true);
            } catch (Exception e){
                Console.WriteLine("Error in directory deletion: " + e.Message);
            }
            finally
            {
                if (Directory.Exists(combined))
                {
                    result = false;
                }
                else result = true;
            }
            return result;
        }


        public static bool Exists(string path, bool dir=false){
            if(!dir)
                return File.Exists(StoragePath + path);
            return Directory.Exists(StoragePath + path);
        }

        public static bool PathContainsDots(string path){
            if ((StoragePath + path).Contains("/.."))
                return true;
            return false;
        }

        public static long DirSize(DirectoryInfo d)
        {
            long size = 0;
            // Add file sizes.
            FileInfo[] fis = d.GetFiles();
            foreach (FileInfo fi in fis)
            {
                size += fi.Length;
            }
            // Add subdirectory sizes.
            DirectoryInfo[] dis = d.GetDirectories();
            foreach (DirectoryInfo di in dis)
            {
                size += DirSize(di);
            }
            return size;
        }

        static public void CreateDirectoryRecursively(string path)
        {
            string[] pathParts = path.Split('\\');

            for (int i = 0; i < pathParts.Length; i++)
            {
                if (i > 0)
                    pathParts[i] = Path.Combine(pathParts[i - 1], pathParts[i]);

                if (!Directory.Exists(pathParts[i]))
                    Directory.CreateDirectory(pathParts[i]);
            }
        }

        public static bool parseConfig()
        {
            StreamReader reader;
            if (File.Exists("storage.config"))
                reader = new StreamReader(File.Open("storage.config", FileMode.Open));
            else
            {
                Console.WriteLine("Configuration file \"storage.config\" is not found");
                return false;
            }
            try
            {
                string line;
                string[] split;
                while (!reader.EndOfStream)
                {
                    line = reader.ReadLine();
                    split = line.Split('=');
                    if (split.Length == 2)
                    {
                        switch (split[0])
                        {
                            case "max_size":
                                {
                                    try
                                    {
                                        MaxSize = Convert.ToInt64(split[1]);
                                        break;
                                    }
                                    catch (Exception e)
                                    {
                                        throw new Exception("Can't parse MaxSize value", e);
                                    }
                                }
                            case "storage_root":
                                storageRoot = split[1];
                                if(storageRoot[storageRoot.Length-1]!='/'){
                                    storageRoot += '/';
                                }
                                break;
                            case "name_server_address":
                                NameServerAddress = split[1];
                                break;
                            default:
                                break;
                        }
                    }
                }

                reader.Close();

                if (MaxSize == 0 || storageRoot == null || NameServerAddress == null)
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    Console.WriteLine("Error in configuration file: " + e.Message + "\nInner Exception: " + e.InnerException.Message);
                else
                    Console.WriteLine("Error in configuration file: " + e.Message);
                reader.Close();
                return false;
            }
            reader.Close();
            return true;
        }

        public static Info GetFileInfo(string path){
            var sizeLong = new System.IO.FileInfo(StoragePath + path).Length;

            return new Info(sizeLong, StorageID );
        }
    }

    public class Info {
        string size;
        string node_id;

        public string Size { get => size; }
        public string Node_id { get => node_id; }

        public Info(Int64 _size, string _node_id){
            size = SizeSuffix(_size);
            node_id = _node_id;
        }

        static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(Int64 value, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }

    public static class Api
    {
        public static HttpResponseMessage Get(string address, string uri)
        {
            HttpResponseMessage result;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + address + "/");
                result = client.GetAsync(uri).Result;
            }
            return result;
        }

        public static HttpResponseMessage Post(HttpContent requestContent, string address, string uri){
            HttpResponseMessage result;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://" + address + "/");
                result = client.PostAsync(uri,requestContent).Result;
            }
            return result;
        }
    }
}
