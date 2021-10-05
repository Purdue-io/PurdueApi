using System.Threading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using PurdueIo.Database;

namespace PurdueIo.Api.Controllers
{
    public class TermController : ODataController
    {
        private ApplicationDbContext dbContext;

        public TermController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 5, MaxAnyAllExpressionDepth = 5)]
        public IActionResult Get(CancellationToken token)
        {
            return Ok(dbContext.Terms);
        }
    }
}