using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace NTAssign
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute(
                name: "Steps",
                url: "{action}",
                defaults: new { controller = "Home" }
            );
            routes.MapRoute(
                name: "DefaultPage",
                url: "",
                defaults: new { controller = "Home", action = "Step1" }
            );
            routes.MapRoute(
                name: "Default2",
                url: "{controller}.mvc/{action}/{id}",
                defaults: new { controller = "Home", action = "Step1", id = UrlParameter.Optional }
            );
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Step1", id = UrlParameter.Optional }
            );
        }
    }
}
