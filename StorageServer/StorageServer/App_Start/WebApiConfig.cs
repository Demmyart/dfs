using System.Net.Http.Headers;
using System.Web.Http;
using System.Web.Http.Cors;

namespace StorageServer
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{path}/{name}/{target}/{user}",
                defaults: new { path = RouteParameter.Optional, name = RouteParameter.Optional, target = RouteParameter.Optional, user = RouteParameter.Optional }
            );

            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
        }
    }
}
