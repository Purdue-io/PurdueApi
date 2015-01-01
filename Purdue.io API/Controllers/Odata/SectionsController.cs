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
using System.Web.Http.Cors;

namespace PurdueIo.Controllers.Odata
{
	[EnableCors(origins: "*", headers: "*", methods: "get")]
	[ODataRoutePrefix("Sections")]
    public class SectionsController : ODataController
    {
		private const int MAX_DEPTH = 3;
        private ApplicationDbContext _Db = new ApplicationDbContext();

        // GET: odata/Sections
		/// <summary>
		/// Returns 404, this call should never be used
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetSections()
        {
            //return db.Sections;
			return Ok(_Db.Sections.AsQueryable());
        }

        // GET: odata/Sections(5)
		/// <summary>
		/// Returns a section given a guid of the section
		/// </summary>
		/// <param name="key">The guid of the section</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({classKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetSection([FromODataUri] Guid key)
        {
            return Ok(SingleResult.Create(_Db.Sections.Where(section => section.SectionId == key)));
        }

		// GET: odata/Sections/Default.ByNumber(Number={[subject][number]})
		/// <summary>
		/// Function to return a list of sections given the subject number
		/// </summary>
		/// <param name="subjectAndNumber">The subject number</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByNumber(Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetSectionsByNumber([FromODataUri] String subjectAndNumber)
		{
			System.Diagnostics.Debug.WriteLine("Inside GetCoursesByNumber");
			Tuple<String, String> course = Utilities.ParseCourse(subjectAndNumber);

			if (course == null)
			{
				//invalid input
				return BadRequest("Invalid format: Course Number is not in the format [Subject][Number] (ex. CS30700)");
			}

			IEnumerable<Section> selectedSections = _Db.Sections
			.Where(
				x =>
					x.Class.Course.Subject.Abbreviation == course.Item1 &&
					x.Class.Course.Number == course.Item2
				);

			return Ok(selectedSections);
		}

		// GET: odata/Sections/Default.ByTerm({term})
		/// <summary>
		/// Function to return a list of sections given the term
		/// </summary>
		/// <param name="term">The term</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByTerm(Term={term})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetSectionsByTerm([FromODataUri] String term)
		{
			String match = Utilities.ParseTerm(term);

			//check if match exists
			if (match == null)
			{
				return BadRequest("Invalid format: Term does not match term format (ex. 201510)");
			}

			IQueryable<Section> selectedSections = _Db.Sections
				.Where(
					x =>
						x.Class.Term.TermCode == match
					);
			return Ok(selectedSections);

		}

		// GET: odata/Sections/Default.ByTermAndNumber(Term={term},Number={subjectAndNumber}
		/// <summary>
		/// Function to return a list of sections given subject number and term
		/// </summary>
		/// <param name="term">The term</param>
		/// <param name="subjectAndNumber">The subject number</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("Default.ByTermAndNumber(Term={term},Number={subjectAndNumber})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetSectionsByTermAndNumber([FromODataUri] String term, [FromODataUri] String subjectAndNumber)
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

			IQueryable<Section> selectedSections = _Db.Sections
					.Where(
						x =>
							x.Class.Term.TermCode == match &&
							x.Class.Course.Subject.Abbreviation == course.Item1 &&
							x.Class.Course.Number == course.Item2
						);

			return Ok(selectedSections);
		}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _Db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool SectionExists(Guid key)
        {
            return _Db.Sections.Count(e => e.SectionId == key) > 0;
        }
    }
}
