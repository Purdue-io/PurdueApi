using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurdueIo.Controllers;
using System.Collections.Generic;
using PurdueIo.Models.Catalog;
using System.Linq;
using System.Web.Http.Results;

namespace PurdueIo.Tests.Controllers
{
	[TestClass]
	public class CatalogControllerTest : TestHarness
	{
		[ClassInitialize]
		public static new void SetUp(TestContext context)
		{
			TestHarness.SetUp(context);
		}

		[TestMethod]
		public void TestFetchAllCourses()
		{
			GenerateTestSchool();
			var controller = new CatalogController();
			var response = controller.Get() as OkNegotiatedContentResult<IEnumerable<CourseViewModel>>;
			var courses = response.Content.ToList();
			
			Assert.IsTrue(courses.Count > 0, "Course request returned no courses.");
			Assert.IsTrue(courses.Where(c => c.Title.Equals("Software Engineering")).Count() > 0, "Software Engineering course wasn't returned.");
		}
	}
}
