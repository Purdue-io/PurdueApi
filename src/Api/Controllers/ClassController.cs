using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using PurdueIo.Database;

namespace PurdueIo.Api.Controllers
{
    public class ClassController : ODataController
    {
        private ApplicationDbContext dbContext;

        public ClassController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 3)]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(dbContext.Classes);
        }
    }
}