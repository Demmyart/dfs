using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace StorageServer
{
    public class ClientController : ApiController
    {
        [HttpPost, ActionName("upload")]
        public async Task<IHttpActionResult> Upload([FromUri] string path, [FromUri] string name, [FromUri] string user)
        {
            if (user == "" || user == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (FileSystem.Exists(path + '/' + user+ '/' + name))
            {
                throw new HttpResponseException(HttpStatusCode.Conflict);
            }

            string uploadingFileName = "";
            string originalFileName = "";

            var size = Convert.ToInt64(Request.Content.Headers.GetValues("Content-Length").First());
            if (FileSystem.FreeSpace - size < 0){
                throw new HttpResponseException(HttpStatusCode.RequestEntityTooLarge);
            };

            try
            {   
                FileSystem.CreateDirectoryRecursively(FileSystem.StoragePath + user + '/' + path);

                var provider = new MultipartFormDataStreamProvider(FileSystem.StoragePath + user + '/' + path);
                var content = new StreamContent(HttpContext.Current.Request.GetBufferlessInputStream(true));
                foreach (var header in Request.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }


                await content.ReadAsMultipartAsync(provider);

                uploadingFileName = provider.FileData.Select(x => x.LocalFileName).FirstOrDefault();
                originalFileName = FileSystem.StoragePath + user + '/' + path + '/' + name;
                if (!File.Exists(originalFileName))
                {
                    File.Move(uploadingFileName, originalFileName);
                }
                else
                {
                    throw new HttpResponseException(HttpStatusCode.Conflict);
                }

                var response = Api.Get(FileSystem.NameServerAddress, String.Format("filereg?path={0}&name={1}&id={2}&free_space={3}&size={4}&user={5}",
                                                                                   path,
                                                                                   name,
                                                                                   FileSystem.StorageID,
                                                                                   (FileSystem.FreeSpace - size).ToString(),
                                                                                   size.ToString(),
                                                                                   user));

                if (response.StatusCode != HttpStatusCode.OK){
                    throw new HttpResponseException(response.StatusCode);
                } else {
                    FileSystem.FreeSpace -= size;
                }


            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    Console.WriteLine("Error in file upload: " + e.Message + "\nInner Exception: " + e.InnerException.Message);
                else
                    Console.WriteLine("Error in file upload: " + e.Message);
                if (uploadingFileName != ""){
                    if (File.Exists(uploadingFileName))
                        File.Delete(uploadingFileName);
                }
                if (originalFileName != "")
                {
                    if (File.Exists(originalFileName))
                        File.Delete(originalFileName);
                }

                throw e;
            }
            return Ok();
        }


        /*

        [HttpPost, ActionName("rewrite")]
        public async Task<IHttpActionResult> Rewrite([FromUri] string path, [FromUri] string name)
        {
            if (!FileSystem.Exists(path + '/' + name))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            string uploadingFileName = "";
            string originalFileName = "";

            try
            {
                FileSystem.CreateDirectoryRecursively(FileSystem.StoragePath + path);

                var provider = new MultipartFormDataStreamProvider(FileSystem.StoragePath + path);
                var content = new StreamContent(HttpContext.Current.Request.GetBufferlessInputStream(true));
                foreach (var header in Request.Content.Headers)
                {
                    content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }


                await content.ReadAsMultipartAsync(provider);

                uploadingFileName = provider.FileData.Select(x => x.LocalFileName).FirstOrDefault();
                originalFileName = FileSystem.StoragePath + path + '/' + name;
                var new_size = new System.IO.FileInfo(uploadingFileName).Length;


                var size_diff = new_size - new System.IO.FileInfo(originalFileName).Length;

                var response = Api.Get(FileSystem.NameServerAddress, String.Format("fileRewrite?path={0}&name={1}&id={2}&free_space={3}&new_size={4}", path, name, FileSystem.StorageID, (FileSystem.FreeSpace - size_diff).ToString(), new_size.ToString()));

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new HttpResponseException(response.StatusCode);
                }
                else
                {
                    File.Delete(originalFileName);
                    File.Move(uploadingFileName, originalFileName);
                    FileSystem.FreeSpace -= size_diff;
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    Console.WriteLine("Error in file rewrite: " + e.Message + "\nInner Exception: " + e.InnerException.Message);
                else
                    Console.WriteLine("Error in file rewrite: " + e.Message);
                if (uploadingFileName != "")
                {
                    if (File.Exists(uploadingFileName))
                        File.Delete(uploadingFileName);
                }

                throw e;
            }
            return Ok();
        }

        */
       

        [HttpGet, ActionName("read")]
        public HttpResponseMessage Read([FromUri] string path, [FromUri] string user)
        {
            if (user == "" || user == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var fullPath = user + "/" + path;
            if (FileSystem.PathContainsDots(fullPath))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (!FileSystem.Exists(fullPath))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }


            var response = Request.CreateResponse();
            response.Content = new PushStreamContent(async (Stream outputStream, HttpContent content, TransportContext context) =>
            {
                try
                {
                    var buffer = new byte[65536];

                    using (var file = FileSystem.GetFileStream(fullPath))
                    {
                        var length = file.Length;
                        var bytesRead = 1;

                        while (length > 0 && bytesRead > 0)
                        {
                            bytesRead = file.Read(buffer, 0, (int)Math.Min(length, (long)buffer.Length));
                            await outputStream.WriteAsync(buffer, 0, bytesRead);
                            length -= bytesRead;
                        }
                    }
                }
                finally
                {
                    outputStream.Close();
                }
            });

            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = fullPath.Split('/').Last();
            return response;
        }




        [HttpGet, ActionName("fileinfo")]
        public IHttpActionResult FileInfo([FromUri] string path){
            if (FileSystem.PathContainsDots(path))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (!FileSystem.Exists(path))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return Ok(FileSystem.GetFileInfo(path));
        }
    }
}