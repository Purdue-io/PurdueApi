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

		[TestMethod]
		public void TestFetchSpecifiedCourses()
		{
			GenerateTestSchool();

			//Using vars cause it looks prettier :D
			//Correct input: CS30700
			var controller = new CatalogController();
			var response = controller.Get("CS30700") as OkNegotiatedContentResult<IEnumerable<CourseViewModel>>;
			var courses = response.Content.ToList();

			Assert.IsTrue(courses.Count > 0, "Course request returned no courses, CS30700 should exist in the catalog.");
			Assert.IsTrue(courses.Where(x => x.Subject.Abbreviation == "CS" && x.Number == "30700").Count() > 0, "CS30700 courses wasn't returned.");

			//Correct input but shortened: CS307
			response = controller.Get("CS307") as OkNegotiatedContentResult<IEnumerable<CourseViewModel>>;
			courses = response.Content.ToList();

			Assert.IsTrue(courses.Count > 0, "Course request returned no courses, CS307 should exist in the catalog.");
			Assert.IsTrue(courses.Where(x => x.Subject.Abbreviation == "CS" && x.Number == "30700").Count() > 0, "CS307 courses wasn't returned.");

			//Invalid class: POOP10100
			response = controller.Get("POOP10100") as OkNegotiatedContentResult<IEnumerable<CourseViewModel>>;
			courses = response.Content.ToList();

			Assert.IsTrue(courses.Count == 0, "Course request should return no classes, POOP10100 should not be a valid course.");

			//Invalid class but shortened: POOP101
			response = controller.Get("POOP101") as OkNegotiatedContentResult<IEnumerable<CourseViewModel>>;
			courses = response.Content.ToList();

			Assert.IsTrue(courses.Count == 0, "Course request should return no classes, POOP101 should not be a valid course.");

			//Format check: CS30777
			var badResponse = controller.Get("CS30777");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "CS30777 is not an valid course");

			//Format check: CS 30700
			badResponse = controller.Get("CS 30700");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "CS 30700 is not an valid course");

			//Format check: CS1
			badResponse = controller.Get("CS1");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "CS1 is not an valid course");

			//Format check: CS1111111
			badResponse = controller.Get("CS1111111");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "CS1111111 is not an valid course");

			//Format check: CS
			badResponse = controller.Get("CS");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "CS is not an valid course");

			//Format check: 111
			badResponse = controller.Get("111");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "111 is not an valid course");
		}
	}
}
