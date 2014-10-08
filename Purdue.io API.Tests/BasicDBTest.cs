using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PurdueIo.Models;

namespace PurdueIo.Tests
{
	[TestClass]
	public class BasicDBTest : TestHarness
	{

		[ClassInitialize]
		public static new void SetUp(TestContext context)
		{
			TestHarness.SetUp(context);
		}

		/// <summary>
		/// Test that outputs all courses, classes, and sections in the DB with relevant info.
		/// </summary>
		[TestMethod]
		public void BasicDbTest()
		{
			GenerateTestSchool();
			var courses = Db.Courses;
			System.Diagnostics.Debug.WriteLine("Found " + courses.Count() + " courses.");
			foreach (var course in courses)
			{
				System.Diagnostics.Debug.WriteLine("\t" + course.Title);
				System.Diagnostics.Debug.WriteLine("\t" + course.Subject.Name + " / " + course.Subject.Abbreviation + course.Number);
				System.Diagnostics.Debug.WriteLine("\t" + course.CreditHours.ToString() + " credit hours");
				System.Diagnostics.Debug.WriteLine("\t'" + course.Description + "'");
				System.Diagnostics.Debug.WriteLine("\twith " + course.Classes.Count + " classes:");

				foreach (var cclass in course.Classes)
				{
					System.Diagnostics.Debug.WriteLine("\t\t" + cclass.Term.TermCode + " term at " + cclass.Campus.Name);
					System.Diagnostics.Debug.WriteLine("\t\twith " + cclass.Sections.Count + " sections:");

					foreach (var section in cclass.Sections)
					{
						System.Diagnostics.Debug.WriteLine("\t\t\t" + section.CRN + ": " + section.Type + " by " + section.Instructors.First().Name);
						System.Diagnostics.Debug.WriteLine("\t\t\t" + section.Room.Building.ShortCode + section.Room.Number + " @ " + section.StartTime.ToString("t"));
					}
				}
			}
			
		}
	}
}
