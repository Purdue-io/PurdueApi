using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ModelBinding;
using System.Web.OData;
using System.Web.OData.Routing;
using PurdueIo.Models;
using PurdueIo.Models.Catalog;
using System.Text.RegularExpressions;

namespace PurdueIo.Controllers.Odata
{
    /*
    The WebApiConfig class may require additional changes to add a route for this controller. Merge these statements into the Register method of the WebApiConfig class as applicable. Note that OData URLs are case sensitive.

    using System.Web.Http.OData.Builder;
    using System.Web.Http.OData.Extensions;
    using PurdueIo.Models.Catalog;
    ODataConventionModelBuilder builder = new ODataConventionModelBuilder();
    builder.EntitySet<Course>("Courses");
    builder.EntitySet<Class>("Classes"); 
    builder.EntitySet<Subject>("Subjects"); 
    config.Routes.MapODataServiceRoute("odata", "odata", builder.GetEdmModel());
    */
	[ODataRoutePrefix("Courses")]
	public class CoursesController : ODataController
    {
		//Regex capture group names
		private const string COURSE_SUBJECT_CAPTURE_GROUP_NAME	= "subject";
		private const string COURSE_NUMBER_CAPTURE_GROUP_NAME	= "number";
		private const string TERM_CAPTURE_GROUP_NAME			= "term";

		//Regex strings
		private const string COURSE_SUBJECT_NUMBER_REGEX		= @"\A(?<" + COURSE_SUBJECT_CAPTURE_GROUP_NAME + @">[A-Za-z]+)(?<" + COURSE_NUMBER_CAPTURE_GROUP_NAME + @">\d{3}(?:00)?)\z";
		private const string TERM_REGEX							= @"\A(?<" + TERM_CAPTURE_GROUP_NAME + @">\d{6})\z";

        private ApplicationDbContext _Db = new ApplicationDbContext();

		/*
		 * Restful call
		 */

        // GET: odata/Courses
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCourses()
        {
			IQueryable a;
			return Ok(_Db.Courses.AsQueryable());
        }

        // GET: odata/Courses({GUID})
		[HttpGet]
		[ODataRoute("({courseKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCourse([FromODataUri] Guid courseKey)
        {
            return Ok(SingleResult.Create(_Db.Courses.Where(course => course.CourseId == courseKey)));
        }

		// GET: odata/Courses/Default.ByNumber(Number={[subject][number]})
		[HttpGet]
		[ODataRoute("Default.ByNumber(Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCoursesByNumber([FromODataUri] String subjectAndNumber)
		{
			System.Diagnostics.Debug.WriteLine("Inside GetCoursesByNumber");
			Tuple<String, String> course = this.ParseCourse(subjectAndNumber);

			if (course == null)
			{
				//invalid input
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			//It's course
			IEnumerable<Course> selectedCourses = _Db.Courses
			.Where(
				x =>
					x.Number == course.Item2 &&
					x.Subject.Abbreviation == course.Item1
				);

			return Ok(selectedCourses);
		}

		// GET: odata/Courses/Default.ByTerm({term})
		[HttpGet]
		[ODataRoute("Default.ByTerm(Term={term})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult ByTerm([FromODataUri] String term)
		{
			System.Diagnostics.Debug.WriteLine("Inside GetCoursesByTerm");
			//Look for term first
			Regex regex = new Regex(TERM_REGEX);
			Match match = regex.Match(term);

			//check if match exists
			if (!match.Success)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}

			//It's term
			String termValue = match.Groups[TERM_CAPTURE_GROUP_NAME].Value;

			IQueryable<Course> selectedCourses = _Db.Classes
				.Where(
					x =>
						x.Term.TermCode == termValue
					).Select(
						x =>
							x.Course).Distinct();
			return Ok(selectedCourses);
			
		}

		// GET: odata/Courses/Default.ByTermAndNumber(Term={term},Number={subjectAndNumber}
		[HttpGet]
		[ODataRoute("Default.ByTermAndNumber(Term={term},Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCoursesByTermAndNumber([FromODataUri] String term, [FromODataUri] String subjectAndNumber)
		{
			Tuple<String, String> course = this.ParseCourse(subjectAndNumber);
			Regex regex = new Regex(TERM_REGEX);
			Match match = regex.Match(term);
			
			if(course == null)
			{
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			if(!match.Success)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}

			IQueryable<Course> selectedCourses = _Db.Classes
					.Where(
						x =>
							x.Term.TermCode == term &&
							x.Course.Subject.Abbreviation == course.Item1 &&
							x.Course.Number == course.Item2
						).Select(
							x =>
								x.Course
							).Distinct();

				return Ok(selectedCourses);
		}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _Db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CourseExists(Guid key)
        {
            return _Db.Courses.Count(e => e.CourseId == key) > 0;
        }

		/// <summary>
		/// Helper function used to parse course input in the format of [CourseSubject][CourseNumber] (ex. MA261).  Returns null if the input is not in the format.
		/// </summary>
		/// <param name="course"></param>
		/// <returns></returns>
		private Tuple<String, String> ParseCourse(String course)
		{
			//Init regex
			Regex regex = new Regex(COURSE_SUBJECT_NUMBER_REGEX);
			Match match = regex.Match(course);

			//If there are no matches, exit with error
			if (!match.Success)
			{
				//error, invalid format
				return null;
			}

			//Capture subject and number group from the string
			String courseSubject = match.Groups[COURSE_SUBJECT_CAPTURE_GROUP_NAME].Value;
			String courseNumber = match.Groups[COURSE_NUMBER_CAPTURE_GROUP_NAME].Value;

			//Add zeros to number if number is only 3 characters (ex. 390 -> 39000)
			if (courseNumber.Length == 3)
			{
				courseNumber += "00";
			}

			return new Tuple<string, string>(courseSubject, courseNumber);
		}
    }
}
