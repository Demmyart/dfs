using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

namespace StorageServer.Controllers
{
    public class StorageController : ApiController
    {
        [HttpPost, ActionName("replicate")]
        public IHttpActionResult Replicate([FromUri] string path, [FromUri] string target)
        {

            return Ok();
        }

        [HttpGet, ActionName("deletedir")]
        public IHttpActionResult DeleteDir([FromUri] string path)
        {
            if (FileSystem.PathContainsDots(path) || path=="" || path=="/")
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            if (!FileSystem.Exists(path,true))
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            if (!FileSystem.DeleteDir(path)){
                throw new HttpResponseException(HttpStatusCode.InternalServerError);
            }

            return Ok();
        }


    }
}
