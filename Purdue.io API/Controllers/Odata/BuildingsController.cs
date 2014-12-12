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
	[ODataRoutePrefix("Buildings")]
	public class BuildingsController : ODataController
	{
		private const int MAX_DEPTH = 2;
		private ApplicationDbContext db = new ApplicationDbContext();

		// GET: odata/Buildings
		/// <summary>
		/// Returns all buildings in existance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetBuildings()
		{
			return Ok(db.Buildings);
		}

		// GET: odata/Buildings(5)
		/// <summary>
		/// Returns a building given the guid
		/// </summary>
		/// <param name="buildingKey"></param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({buildingKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = MAX_DEPTH, MaxExpansionDepth = MAX_DEPTH)]
		public IHttpActionResult GetBuilding([FromODataUri] Guid buildingKey)
		{
			return Ok(SingleResult.Create(db.Buildings.Where(building => building.BuildingId == buildingKey)));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private bool BuildingExists(Guid key)
		{
			return db.Buildings.Count(e => e.BuildingId == key) > 0;
		}
	}
}
