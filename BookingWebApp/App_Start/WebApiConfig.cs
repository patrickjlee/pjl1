using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace BookingWebApp
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();

            //config.Routes.MapHttpRoute(
            //    name: "DefaultApi",
            //    routeTemplate: "api/{controller}/{id}",
            //    defaults: new { id = RouteParameter.Optional }
            //);


            //// Added because we have a composite key in BookingsController
            //config.Routes.MapHttpRoute(
            //    name: "BookingsCompoundKeyApi",
            //    routeTemplate: "api/{controller}/{PartitionKey}/{RowKey}",
            //    defaults: new { PartitionKey = RouteParameter.Optional, RowKey = RouteParameter.Optional }
            //);

            config.Routes.MapHttpRoute(
                name: "DefaultApi2",
                routeTemplate: "api/{controller}/{PartitionKey}",
                defaults: new { PartitionKey = RouteParameter.Optional }
            );




        }
    }
}
