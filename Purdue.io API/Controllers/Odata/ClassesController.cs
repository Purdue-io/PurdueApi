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

namespace PurdueIo.Controllers.Odata
{
	[ODataRoutePrefix("Classes")]
    public class ClassesController : ODataController
    {
		private const int MAX_DEPTH = 4;
        private ApplicationDbContext _Db = new ApplicationDbContext();

        //GET: odata/Classes
		/// <summary>
		/// Returns 404, this call should never be used
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetClasses()
		{
			//return db.Classes;
			return Ok(_Db.Classes.AsQueryable());
		}

        // GET: odata/Classes({GUID})
		/// <summary>
		/// Returns a class given a guid of the class
		/// </summary>
		/// <param name="classKey"></param>
		/// <returns>the guid of the class</returns>
		[HttpGet]
		[ODataRoute("({classKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetClass([FromODataUri] Guid classKey)
        {
			//For those confused we use @class because class is a keyword
            return Ok(SingleResult.Create(_Db.Classes.Where(@class => @class.ClassId == classKey)));
        }

		// GET: odata/Classes/Default.ByNumber(Number={[subject][number]})
		/// <summary>
		/// Function to return a list of classes given the subject number
		/// </summary>
		/// <param name="subjectAndNumber">The subject number</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByNumber(Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetClassesByNumber([FromODataUri] String subjectAndNumber)
		{
			System.Diagnostics.Debug.WriteLine("Inside GetCoursesByNumber");
			Tuple<String, String> course = Utilities.ParseCourse(subjectAndNumber);

			if (course == null)
			{
				//invalid input
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			IEnumerable<Class> selectedClasses = _Db.Classes
			.Where(
				x =>
					x.Course.Subject.Abbreviation == course.Item1 &&
					x.Course.Number == course.Item2
				);

			return Ok(selectedClasses);
		}

		// GET: odata/Classes/Default.ByTerm({term})
		/// <summary>
		/// Function to return a list of classes given the term
		/// </summary>
		/// <param name="term">The term</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByTerm(Term={term})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetClassesByTerm([FromODataUri] String term)
		{
			String match = Utilities.ParseTerm(term);

			//check if match exists
			if (match == null)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}

			IQueryable<Class> selectedClasses = _Db.Classes
				.Where(
					x =>
						x.Term.TermCode == match
					);
			return Ok(selectedClasses);

		}

		// GET: odata/Classes/Default.ByTermAndNumber(Term={term},Number={subjectAndNumber}
		/// <summary>
		/// Function to return a list of sections given subject number and term
		/// </summary>
		/// <param name="term">The term</param>
		/// <param name="subjectAndNumber">The subject number</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByTermAndNumber(Term={term},Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetClassesByTermAndNumber([FromODataUri] String term, [FromODataUri] String subjectAndNumber)
		{
			Tuple<String, String> course = Utilities.ParseCourse(subjectAndNumber);
			String match = Utilities.ParseTerm(term);

			if (course == null)
			{
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			if (match == null)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}

			IQueryable<Class> selectedCourses = _Db.Classes
					.Where(
						x =>
							x.Term.TermCode == match &&
							x.Course.Subject.Abbreviation == course.Item1 &&
							x.Course.Number == course.Item2
						);

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

        private bool ClassExists(Guid key)
        {
            return _Db.Classes.Count(e => e.ClassId == key) > 0;
        }
    }
}
