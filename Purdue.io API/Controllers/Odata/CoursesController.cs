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
using PurdueIoDb;
using PurdueIoDb.Catalog;
using PurdueIo.Utils;
using System.Text.RegularExpressions;

namespace PurdueIo.Controllers.Odata
{
	[ODataRoutePrefix("Courses")]
	public class CoursesController : ODataController
    {
        private ApplicationDbContext _Db = new ApplicationDbContext();

        // GET: odata/Courses
		/// <summary>
		/// Returns all courses since the dawn of time
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCourses()
        {
			return Ok(_Db.Courses.AsQueryable());
        }

        // GET: odata/Courses({GUID})
		/// <summary>
		/// Returns a course given a guid of the course
		/// </summary>
		/// <param name="courseKey">The guid of the course</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({courseKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCourse([FromODataUri] Guid courseKey)
        {
            return Ok(SingleResult.Create(_Db.Courses.Where(course => course.CourseId == courseKey)));
        }

		// GET: odata/Courses/Default.ByNumber(Number={[subject][number]})
		/// <summary>
		/// Function to return a list of courses given the subject number
		/// Ex. /Courses/Default.ByNumber(Number='CS30700')
		/// </summary>
		/// <param name="subjectAndNumber">The subject number</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByNumber(Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCoursesByNumber([FromODataUri] String subjectAndNumber)
		{
			System.Diagnostics.Debug.WriteLine("Inside GetCoursesByNumber");
			Tuple<String, String> course = Utilities.ParseCourse(subjectAndNumber);

			if (course == null)
			{
				//invalid input
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			IEnumerable<Course> selectedCourses = _Db.Courses
			.Where(
				x =>
					x.Number == course.Item2 &&
					x.Subject.Abbreviation == course.Item1
				);

			return Ok(selectedCourses);
		}

		// GET: odata/Courses/Default.ByTerm({term})
		/// <summary>
		/// Function to return a list of courses given the term
		/// </summary>
		/// <param name="term">The term</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByTerm(Term={term})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCoursesByTerm([FromODataUri] String term)
		{
			String match = Utilities.ParseTerm(term);

			//check if match exists
			if (match == null)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}
	
			IQueryable<Course> selectedCourses = _Db.Classes
				.Where(
					x =>
						x.Term.TermCode == match
					).Select(
						x =>
							x.Course).Distinct();
			return Ok(selectedCourses);
			
		}

		// GET: odata/Courses/Default.ByTermAndNumber(Term={term},Number={subjectAndNumber}
		/// <summary>
		/// Function to return a list of courses given subject number and term
		/// </summary>
		/// <param name="term">The term</param>
		/// <param name="subjectAndNumber">The subject number</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByTermAndNumber(Term={term},Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IHttpActionResult GetCoursesByTermAndNumber([FromODataUri] String term, [FromODataUri] String subjectAndNumber)
		{
			Tuple<String, String> course = Utilities.ParseCourse(subjectAndNumber);
			String match = Utilities.ParseTerm(term);
			
			if(course == null)
			{
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			if(match == null)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}

			IQueryable<Course> selectedCourses = _Db.Classes
					.Where(
						x =>
							x.Term.TermCode == match &&
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

		
    }
}
