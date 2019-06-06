using System;
using Nancy;
using System.Net.Http;
using System.Net;
using System.IO;
using System.Collections.Generic;

namespace NamingServer
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected override void ApplicationStartup(Nancy.TinyIoc.TinyIoCContainer container, Nancy.Bootstrapper.IPipelines pipelines)
        {
            //CORS Enable
            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");

            });
        }
    }

    public class ClientAPI : NancyModule
    {
        public ClientAPI() 
        {
            Get["/list"] = param =>
            {
                try
                {
                    Directory directory = FileSystem.GetDirectory(FileSystem.GetFullPathFromUser(Request.Query["user"], Request.Query["path"]));
                    return JsonResponses.GetDirectoryJson(directory);
                }
                catch (Exception err)
                {
                    Console.WriteLine("list: "+err.Message);
                    return 404;
                }
            };
            Get["/createDir"] = param =>
            {
                try
                {
                    Directory penDir = FileSystem.GetDirectory(FileSystem.GetFullPathFromUser(Request.Query["user"], Request.Query["path"]));
                    penDir.CreateSubDir(Request.Query["name"], Request.Query["user"]);
                    FileSystem.db.Log("DIR CREATE", (Request.Query["path"] + "/" + Request.Query["name"] + "/").Replace("//", "/"), Request.Query["user"]);
                    return 200;
                }
                catch (Exception err)
                {
                    Console.WriteLine("createDir: " + err.Message);
                    return 500;
                }
            };
            Get["/delDir"] = param =>
            {
                try
                {
                    Directory penDir = FileSystem.GetDirectory(FileSystem.GetFullPathFromUser(Request.Query["user"], Request.Query["path"]));
                    penDir.DeleteSubDir(Request.Query["name"]);
                    FileSystem.db.Log("DIR DELETE", (Request.Query["path"] + "/" + Request.Query["name"]+"/").Replace("//", "/"), Request.Query["user"]);
                    return 200;
                }
                catch (Exception e)
                {
                    if (e.InnerException != null)
                        Console.WriteLine("Error in directory deletion: " + e.Message + "\nInner Exception: " + e.InnerException.Message);
                    else
                        Console.WriteLine("Error in directory deletion: " + e.Message);
                    return 404;
                }
            };
            Get["/uploadFile"] = param =>
            {
                try
                {
                    return JsonResponses.GetStorageToUpload(FileSystem.db.GetStorageAddressById(FileSystem.db.ChooseMainStorage(Request.Query["size"])));
                }
                catch (Exception err)
                {
                    Console.WriteLine("uploadFile: "+err.Message);
                    return 500;
                }
            };
            Get["/delFile"] = param =>
            {
                try
                {
                    Directory directory = FileSystem.GetDirectory(FileSystem.GetFullPathFromUser(Request.Query["user"], Request.Query["path"]));
                    directory.DeleteFile(Request.Query["name"]);
                    FileSystem.db.Log("FILE DELETE", (Request.Query["path"] + "/" + Request.Query["name"]).Replace("//", "/"), Request.Query["user"]);
                    return 200;
                }
                catch (Exception err)
                {
                    Console.WriteLine("delFile: " + err.Message);
                    return 500;
                }
            };
            Get["/regUser"] = param =>
            {
                try
                {
                    string userName = Request.Query["user"];
                    if (!FileSystem.db.CheckUserExist(userName))
                    {
                        FileSystem.db.ExecuteNonQuery("INSERT INTO users(name) VALUES('" + userName + "')");
                        FileSystem.root.CreateSubDir(userName, userName);
                    }
                    return 200;
                }
                catch (Exception err)
                {
                    Console.WriteLine("regUser: " + err.Message);
                    return 500;
                }
            };
            Get["/getLog"] = param =>
            {
                try
                {
                    return JsonResponses.GetUserLog(Request.Query["user"]);
                }
                catch (Exception err)
                {
                    Console.WriteLine("getLogError: " + err.Message);
                    return 500;
                }
            };
        }
    }

    public class StorageAPI : NancyModule
    {
        public StorageAPI()
        {
            Get["/storageReg"] = param =>
            {
                try
                {
                    if (!FileSystem.db.StorageCheck(Request.UserHostAddress, Request.Query["port"]))
                    {
                        FileSystem.db.ExecuteNonQuery("INSERT INTO storages(id, ip, port, free_space) VALUES('" + Request.Query["id"] + "', '" + Request.UserHostAddress + "', '" + Request.Query["port"] + "', '" + Request.Query["free_space"] + "');");
                        Console.WriteLine("Registered storage server: " + Request.UserHostAddress);
                        return 200;
                    }
                    else
                    {
                        return 500;
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("storageReg: " + err.Message);
                    return 409;
                }
            };
            Get["/storageConn"] = param =>
            {
                try
                {
                    FileSystem.db.ExecuteNonQuery("UPDATE storages SET ip='" + Request.UserHostAddress + "', port='" + Request.Query["port"] + "', free_space='" + Request.Query["free_space"] + "' WHERE id='" + Request.Query["id"] + "'");
                    return 200;
                }
                catch (Exception err)
                {
                    Console.WriteLine("storageConn: " + err.Message);
                    return 500;
                }
            };
            Get["/fileReg"] = param =>
            {
                try
                {
                    string serverId = Request.Query["id"];
                    string fileSize = Request.Query["size"];
                    string userName = Request.Query["user"];
                    Directory dir = FileSystem.GetDirectory(FileSystem.GetFullPathFromUser(userName, Request.Query["path"]));
                    FileSystem.db.UpdateStorageFreeSpace(serverId, fileSize);
                    dir.RegFile(Request.Query["name"], fileSize, serverId, userName);
                    FileSystem.db.Log("FILE UPLOAD",(Request.Query["path"]+"/"+Request.Query["name"]).Replace("//","/"),Request.Query["user"]);
                    return 200;
                }
                catch (Exception err)
                {
                    Console.WriteLine("fileReg: " + err.Message);
                    return 500;
                }
            };
        }

        public static int GetRequest(string address, string uri)
        {
            HttpWebRequest http = (HttpWebRequest)WebRequest.Create("http://" + address + "/" + uri);
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)http.GetResponse();
            }catch (WebException e){
                response = (HttpWebResponse)e.Response;
            }
            int status = (int)response.StatusCode;
            response.Dispose();
            return status;
        }
    }
}
