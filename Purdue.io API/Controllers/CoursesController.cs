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
	public class CoursesController : ODataController
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: odata/Courses
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IQueryable<Course> GetCourses()
        {
			return db.Courses.AsQueryable() ;
        }

        // GET: odata/Courses(5)
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public SingleResult<Course> GetCourse([FromODataUri] Guid key)
        {
            return SingleResult.Create(db.Courses.Where(course => course.CourseId == key));
        }

        // GET: odata/Courses(5)/Classes
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public IQueryable<Class> GetClasses([FromODataUri] Guid key)
        {
            return db.Courses.Where(m => m.CourseId == key).SelectMany(m => m.Classes);
        }

        // GET: odata/Courses(5)/Subject
		[EnableQuery(MaxAnyAllExpressionDepth = 3)]
		public SingleResult<Subject> GetSubject([FromODataUri] Guid key)
        {
            return SingleResult.Create(db.Courses.Where(m => m.CourseId == key).Select(m => m.Subject));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CourseExists(Guid key)
        {
            return db.Courses.Count(e => e.CourseId == key) > 0;
        }
    }
}
