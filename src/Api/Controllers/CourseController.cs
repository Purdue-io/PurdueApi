using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using PurdueIo.Database;

namespace PurdueIo.Api.Controllers
{
    public class CourseController : ODataController
    {
        private ApplicationDbContext dbContext;

        public CourseController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        [EnableQuery(MaxExpansionDepth = EdmModelBuilder.MAX_EXPAND_DEPTH,
            MaxAnyAllExpressionDepth = EdmModelBuilder.MAX_EXPAND_DEPTH)]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(dbContext.Courses);
        }
    }
}