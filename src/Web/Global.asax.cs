using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Web.Code;

namespace Web
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class WebApiApplication : System.Web.HttpApplication
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(WebApiApplication));
        Timer crawlMediaSources;

        protected void Application_Start()
        {
            log4net.Config.XmlConfigurator.Configure();
            logger.Info("LDSTorrents.Web is starting...");

            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            logger.Info("Starting crawler...");
            var daily = Convert.ToInt32(TimeSpan.FromDays(1).TotalMilliseconds);
            crawlMediaSources = new Timer(
                new TimerCallback(Crawler.ScrapeChannels), 
                new HttpContextWrapper(HttpContext.Current), 
                0, 
                daily
            );
            
        }

        protected void Application_End()
        {
            logger.Info("LDSTorrents.Web is shutting down...");
            crawlMediaSources.Dispose();
        }
    }
}