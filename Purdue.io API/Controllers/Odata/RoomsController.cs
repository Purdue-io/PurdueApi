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
	[ODataRoutePrefix("Rooms")]
	public class RoomsController : ODataController
	{
		private ApplicationDbContext db = new ApplicationDbContext();

		// GET: odata/Rooms
		/// <summary>
		/// Returns all rooms in existance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetRooms()
		{
			return Ok(db.Rooms);
		}

		// GET: odata/Rooms(5)
		/// <summary>
		/// Returns a room given the guid
		/// </summary>
		/// <param name="roomKey"></param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({roomKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetRoom([FromODataUri] Guid roomKey)
		{
			return Ok(SingleResult.Create(db.Rooms.Where(room => room.RoomId == roomKey)));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private bool RoomExists(Guid key)
		{
			return db.Rooms.Count(e => e.RoomId == key) > 0;
		}
	}
}
