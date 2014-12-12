using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Optimization;

namespace PurdueIo
{
	public class BundleConfig
	{
		// For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
		public static void RegisterBundles(BundleCollection bundles)
		{
			bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
				"~/Scripts/jquery-{version}.js"));

			// Use the development version of Modernizr to develop with and learn from. Then, when you're
			// ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
			bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
				"~/Scripts/modernizr-*"));

			bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
				"~/Scripts/bootstrap.js",
				"~/Scripts/respond.js"));

			bundles.Add(new ScriptBundle("~/bundles/js").Include(
				"~/Scripts/jquery-{version}.js"));

			bundles.Add(new ScriptBundle("~/bundles/ts").Include(
				"~/Scripts/ts/queryTester.js",
                "~/Scripts/ts/DataSource.js"));

			bundles.Add(new StyleBundle("~/Content/css")
				.Include("~/Content/css/Fonts.css", new CssRewriteUrlTransform())
				.Include("~/Content/css/Demo.css", new CssRewriteUrlTransform()));

			// Set EnableOptimizations to false for debugging. For more information,
			// visit http://go.microsoft.com/fwlink/?LinkId=301862
#if !DEBUG
			BundleTable.EnableOptimizations = true;
#endif
		}
	}
}
