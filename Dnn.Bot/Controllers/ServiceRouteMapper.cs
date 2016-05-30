using System.Web.Http;
using DotNetNuke.Web.Api;

namespace Dnn.Bot.Controllers
{
    public class ServiceRouteMapper : IServiceRouteMapper
    {
        public void RegisterRoutes(IMapRoute routeManager)
        {
            routeManager.MapHttpRoute("DnnBot",
                                    "default",
                                    "{controller}/{id}",
                                    new { id = RouteParameter.Optional },
                                    new[] { "Dnn.Bot.Controllers" });

        }
    }
}