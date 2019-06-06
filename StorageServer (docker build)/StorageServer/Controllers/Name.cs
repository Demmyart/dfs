using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

namespace StorageServer.Controllers
{
    public class NameController : ApiController
    {
        public NameController()
        {

        }

        [HttpGet, ActionName("replicate")]
        public async Task<IHttpActionResult> Replicate([FromUri] string path, [FromUri] string target, [FromUri] string name, [FromUri] string user)
        {
            if (user == "" || user == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            string originalFileName = FileSystem.StoragePath+ user + '/' +path+'/'+name;
            string uploadingFileName = "replication_temp/" + user + '/' + name;
            FileSystem.CreateDirectoryRecursively("replication_temp/" + user + '/');

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "http://" + target + "/api/client/read?path=" + path + '/' + name + "&user=" + user;
                    using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                    using (Stream streamDownload = await response.Content.ReadAsStreamAsync())
                    {
                        if (!response.IsSuccessStatusCode){
                            throw new HttpResponseException(response.StatusCode);
                        }
                        using (Stream streamUpload = File.Open(uploadingFileName, FileMode.Create))
                        {
                            await streamDownload.CopyToAsync(streamUpload);
                        }
                    }
                }

                var size = new System.IO.FileInfo(uploadingFileName).Length;

                if (File.Exists(originalFileName))
                {
                    size -= new System.IO.FileInfo(originalFileName).Length;
                    File.Delete(originalFileName);
                }
                FileSystem.CreateDirectoryRecursively(FileSystem.StoragePath + user + '/' + path + '/');
                File.Move(uploadingFileName, originalFileName);
                FileSystem.FreeSpace -= size;

            }
            catch (Exception e)
            {
                if (e.InnerException != null)
                    Console.WriteLine("Error in file replication: " + e.Message + "\nInner Exception: " + e.InnerException.Message);
                else
                    Console.WriteLine("Error in file replication: " + e.Message);
                if (uploadingFileName != "")
                {
                    if (File.Exists(uploadingFileName))
                        File.Delete(uploadingFileName);
                }

                throw e;
            }

            return Ok( new { free = FileSystem.FreeSpace.ToString()});
        }

        [HttpGet, ActionName("deletedir")]
        public IHttpActionResult DeleteDir([FromUri] string path, [FromUri] string user)
        {
            if (user == "" || user == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var fullPath = user + '/' + path;
            if (FileSystem.PathContainsDots(fullPath) || fullPath=="" || fullPath=="/")
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (!FileSystem.Exists(fullPath,true))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            if (!FileSystem.DeleteDir(fullPath)){
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            return Ok();
        }


        [HttpGet, ActionName("delete")]
        public IHttpActionResult Delete([FromUri] string path, [FromUri] string user)
        {
            if (user == "" || user == null)
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            var fullPath = user + '/' + path;
            if (FileSystem.PathContainsDots(fullPath))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (!FileSystem.Exists(fullPath))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }


            FileSystem.Delete(fullPath);

            return Ok();
        }
    }
}
