using System.Web;
using System.Web.Http;

namespace StorageServer
{
    public class Global : HttpApplication
    {
        public Global(){
            FileSystem.Init();
        }

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
