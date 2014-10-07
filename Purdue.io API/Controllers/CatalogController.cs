using PurdueIo.Models;
using PurdueIo.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;

namespace PurdueIo.Controllers
{
	[RoutePrefix("Catalog")]
    public class CatalogController : ApiController
    {
		// This DB really should be instantiated on its own in each method... but that causes disposed problems.
		private ApplicationDbContext _Db = new ApplicationDbContext(); 

        // GET: Catalog
		[Route("")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult Get()
        {
			// Get all of the courses, convert to viewmodel.
			var allcourses = _Db.Courses.ToList().Select(x => x.ToViewModel());
			// Return w/ Ok status code.
			return Ok<IEnumerable<CourseViewModel>>(allcourses);
        }

        // GET: Catalog/MA261
		[Route("{course}")]
		[ResponseType(typeof (IEnumerable<CourseViewModel>))]
		public IHttpActionResult Get(string course)
        {
			// Not implemented.
			return Ok();
        }
    }
}
