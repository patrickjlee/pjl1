using System.Web;
using System.Web.Mvc;

using System.Web.Http.Filters; // added for AuthorizationFilterAttribute
using System; // added for Uri
using System.Net.Http; // added for HttpResponseMessage
using System.Net; // added for HttpStatusCode

namespace BookingWebApp
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());

            // 26 Sep 2018 PJL Added to prevent non SSL access (except when debugging)
#if DEBUG
#else
                        filters.Add(new RequireHttpsAttribute());
#endif
        }

    }

    // Patrick Lee: added to only allow https access for webapi methods 
    // (as per http://www.codeguru.com/csharp/.net/using-ssl-in-asp.net-web-api.htm)
    // After this any WebApi controllers (or particular methods only if preferred) just need to be 
    // decorated with the [UseSSL] attribute and they will only work if accessed via https
    public class UseSSLAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            if (actionContext.Request.RequestUri.Scheme != Uri.UriSchemeHttps)
            {
                HttpResponseMessage msg = new HttpResponseMessage();
                msg.StatusCode = HttpStatusCode.Forbidden;
                msg.ReasonPhrase = "SSL Needed!";
                actionContext.Response = msg;
            }
            else
            {
                base.OnAuthorization(actionContext);
            }
        }
    } // class

} // namespace
