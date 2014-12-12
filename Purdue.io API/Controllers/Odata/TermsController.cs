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
using System.Web.Http.Cors;

namespace PurdueIo.Controllers.Odata
{
	[EnableCors(origins: "*", headers: "*", methods: "get")]
	[ODataRoutePrefix("Terms")]
	public class TermsController : ODataController
	{
		private ApplicationDbContext db = new ApplicationDbContext();

		// GET: odata/Terms
		/// <summary>
		/// Returns all terms in existance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetTerms()
		{
			return Ok(db.Terms);
		}

		// GET: odata/Terms(5)
		/// <summary>
		/// Returns a term given the guid
		/// </summary>
		/// <param name="termKey"></param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({termKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetTerm([FromODataUri] Guid termKey)
		{
			return Ok(SingleResult.Create(db.Terms.Where(term => term.TermId == termKey)));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private bool TermExists(Guid key)
		{
			return db.Terms.Count(e => e.TermId == key) > 0;
		}
	}
}
