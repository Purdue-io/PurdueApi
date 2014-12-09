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
	[ODataRoutePrefix("Instructors")]
    public class InstructorsController : ODataController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: odata/Instructors
		/// <summary>
		/// Returns all instructors in exsistance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetInstructors()
        {
            return Ok(db.Instructors);
        }

        // GET: odata/Instructors({GUID})
		/// <summary>
		/// Returns an instructor given the guid
		/// </summary>
		/// <param name="instructorKey">The guid of the instructor</param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({instructorKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetInstructor([FromODataUri] Guid instructorKey)
        {
			return Ok(SingleResult.Create(db.Instructors.Where(instructor => instructor.InstructorId == instructorKey)));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool InstructorExists(Guid key)
        {
            return db.Instructors.Count(e => e.InstructorId == key) > 0;
        }
    }
}
