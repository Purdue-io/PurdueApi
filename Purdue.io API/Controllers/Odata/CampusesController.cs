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
	[ODataRoutePrefix("Campuses")]
    public class CampusesController : ODataController
    {
		private const int MAX_DEPTH = 3;
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: odata/Campuses
		/// <summary>
		/// Returns all campuses in existance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetCampuses()
        {
            return Ok(db.Campuses);
        }

        // GET: odata/Campuses(5)
		/// <summary>
		/// Returns a campus given the guid
		/// </summary>
		/// <param name="campusKey"></param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({campusKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetCampus([FromODataUri] Guid campusKey)
        {
            return Ok(SingleResult.Create(db.Campuses.Where(campus => campus.CampusId == campusKey)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CampusExists(Guid key)
        {
            return db.Campuses.Count(e => e.CampusId == key) > 0;
        }
    }
}
