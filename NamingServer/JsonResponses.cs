using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace NamingServer
{
    public static class JsonResponses
    {
        public static string GetDirectoryJson(Directory directory)
        {
            dynamic dirJson = new JObject();
            dirJson.name = directory.Name;
            dirJson.parentPath = FileSystem.GetUserPathFromFull(directory.Owner, directory.ParentPath);
            dirJson.currentPath = FileSystem.GetUserPathFromFull(directory.Owner, directory.CurrentPath);
            dirJson.items = new JArray();
            foreach (KeyValuePair<string, Directory>subdir in directory.Directories)
            {
                dynamic item = new JObject();
                item.name = subdir.Key;
                item.type = "dir";
                item.owner = subdir.Value.Owner;
                dirJson.items.Add(item);
            }
            foreach (KeyValuePair<string, DirFile> file in directory.Files)
            {
                dynamic item = new JObject();
                item.name = file.Key;
                item.type = "file";
                item.owner = file.Value.Owner;
                item.address = file.Value.Address;
                item.reserveAddress = file.Value.ReserveAddress;
                item.size = SizeSuffix(file.Value.Size);
                dirJson.items.Add(item);
            }
            return dirJson.ToString();
        }

        public static string GetUserLog(string user)
        {
            dynamic logJson = new JObject();

            var logs = FileSystem.db.GetLog(user);
            logJson.items = new JArray();
            for (int i = logs.Count - 1; i >= 0;--i)
            {
                dynamic item = new JObject();
                item.path = logs[i][0];
                item.user = logs[i][1];
                item.time = logs[i][2];
                item.action = logs[i][3];
                logJson.items.Add(item);
            }

            return logJson.ToString();
        }

        public static string GetStorageToUpload(string storage)
        {
            dynamic storJson = new JObject();
            storJson.mainStorage = storage;
            return storJson.ToString();
        }

        static readonly string[] SizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(string valueStr, int decimalPlaces = 2)
        {
            var value=Convert.ToInt64(valueStr);
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix((-value).ToString()); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            int mag = (int)Math.Log(value, 1024);

            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

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
}
