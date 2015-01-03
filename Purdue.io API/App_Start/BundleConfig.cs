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
			var scriptBundle = new ScriptBundle("~/bundles/ts");
			scriptBundle.Include(
				"~/Scripts/prettify.js",
				"~/Scripts/es6-promise-2.0.1.js",
                "~/Scripts/ts/JsonRequest.js",
				"~/Scripts/ts/QueryTester.js");
#if DEBUG
			scriptBundle.Include("~/Scripts/ts/DEBUG.js");
#endif

			bundles.Add(scriptBundle);

			bundles.Add(new StyleBundle("~/Content/bundles/css")
				.Include("~/Content/css/sunburst.css", new CssRewriteUrlTransform())
				.Include("~/Content/css/prettify.css", new CssRewriteUrlTransform())
				.Include("~/Content/css/Demo.css", new CssRewriteUrlTransform()));

			// Set EnableOptimizations to false for debugging. For more information,
			// visit http://go.microsoft.com/fwlink/?LinkId=301862
#if !DEBUG
			BundleTable.EnableOptimizations = true;
#endif
		}
	}
}
