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

		[TestMethod]
		public void TestFetchSpecifiedClasses()
		{
			//Ignoring checks on course since is it checked in TestFetchSpecifiedCourses()
			GenerateTestSchool();

			Guid testClassId = this.Db.Classes
				.Where(
					x =>
						x.Course.Number == "30700" &&
						x.Course.Subject.Abbreviation == "CS"
						).First().ClassId;

			//Using vars cause it looks prettier :D
			//Correct input: CS30700/ClassGUID
			var controller = new CatalogController();
			var response = controller.Get("CS30700", testClassId.ToString()) as OkNegotiatedContentResult<IEnumerable<ClassViewModel>>;
			var classes = response.Content.ToList();

			Assert.IsTrue(classes.Count > 0, "Class request returned no classes, CS30700/" + testClassId.ToString() + " should exist in the catalog.");
			Assert.IsTrue(classes
				.Where(
					x => 
						x.Course.Subject.Abbreviation == "CS" && 
						x.Course.Number == "30700" &&
						x.ClassId == testClassId
					).Count() > 0, "CS30700/" + testClassId.ToString() +" classes wasn't returned.");

			//Incorrect input: CS30700/0
			var badResponse = controller.Get("CS30700", "0");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "0 is not an valid class id");

			//Incorrect input: CS30700/d
			badResponse = controller.Get("CS30700", "d");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "d is not an valid class id");

		}

		[TestMethod]
		public void TestFetchSpecifiedSections()
		{
			//Ignoring checks on course since is it checked in TestFetchSpecifiedCourses()
			//Ignoring checks on class since is it checked in TestFetchSpecifiedClasses()

			//Ignoring checks on course since is it checked in TestFetchSpecifiedCourses()
			GenerateTestSchool();

			Guid testClassId = this.Db.Classes
				.Where(
					x =>
						x.Course.Number == "30700" &&
						x.Course.Subject.Abbreviation == "CS"
						).First().ClassId;

			Guid testSectionId = this.Db.Sections
				.Where(
					x =>
						x.Class.Course.Number == "30700" &&
						x.Class.Course.Subject.Abbreviation == "CS" &&
						x.Class.ClassId == testClassId
						).First().SectionId;

			//Using vars cause it looks prettier :D
			//Correct input: CS30700/ClassGUID/SectionGUID
			var controller = new CatalogController();
			var response = controller.Get("CS30700", testClassId.ToString(), testSectionId.ToString()) as OkNegotiatedContentResult<IEnumerable<SectionViewModel>>;
			var sections = response.Content.ToList();

			Assert.IsTrue(sections.Count > 0, "Class request returned no sections, CS30700/" + testClassId.ToString() + "/" + testSectionId.ToString() + " should exist in the catalog.");
			Assert.IsTrue(sections
				.Where(
					x =>
						x.Class.Course.Subject.Abbreviation == "CS" &&
						x.Class.Course.Number == "30700" &&
						x.Class.ClassId == testClassId &&
						x.SectionId == testSectionId
					).Count() > 0, "CS30700/" + testClassId.ToString() + "/" + testSectionId.ToString() + " sections wasn't returned.");

			//Incorrect input: CS30700/ClassGUID/0
			var badResponse = controller.Get("CS30700", testClassId.ToString(), "0");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "0 is not an valid section id");

			//Incorrect input: CS30700/ClassGUID/d
			badResponse = controller.Get("CS30700", testClassId.ToString(), "d");
			Assert.IsInstanceOfType(badResponse, typeof(BadRequestErrorMessageResult), "d is not an valid section id");
		}
	}
}
