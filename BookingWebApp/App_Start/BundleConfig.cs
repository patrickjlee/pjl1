﻿using System.Web;
using System.Web.Optimization;

namespace BookingWebApp
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      // 01 May 2019 changed to resolve formatting problem after updating bootstrap from 3.3.7 to 4.3.1      
                      // then downloaded the flatly version to bootstrap.min.css and overwrote the file in the project wihh this
                      // and changed to use min.css
                      //// 25 Sep 2018 PJL amended to use theme, Flatly, copied in from https://bootswatch.com/4/flatly/bootstrap.min.css
                      //      //"~/Content/bootstrap.css",
                      //      "~/Content/bootstrap-theme.min.css",
                      "~/Content/bootstrap.min.css",

                      "~/Content/site.css"));
        }
    }
}
