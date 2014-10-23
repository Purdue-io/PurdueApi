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

namespace PurdueIo.Controllers.Odata
{
	[ODataRoutePrefix("Subjects")]
	public class SubjectController : ODataController
	{
		private ApplicationDbContext db = new ApplicationDbContext();

		// GET: odata/Subject
		/// <summary>
		/// Returns all subjects in existance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetSubjects()
		{
			return Ok(db.Subjects);
		}

		// GET: odata/Subject(5)
		/// <summary>
		/// Returns a subject given the guid
		/// </summary>
		/// <param name="subjectKey"></param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({subjectKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetSubject([FromODataUri] Guid subjectKey)
		{
			return Ok(SingleResult.Create(db.Subjects.Where(subject => subject.SubjectId == subjectKey)));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private bool SubjectExists(Guid key)
		{
			return db.Subjects.Count(e => e.SubjectId == key) > 0;
		}
	}
}
