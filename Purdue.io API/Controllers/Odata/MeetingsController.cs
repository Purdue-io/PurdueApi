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
	[ODataRoutePrefix("Meetings")]
	public class MeetingsController : ODataController
	{
		private ApplicationDbContext db = new ApplicationDbContext();

		// GET: odata/Meetings
		/// <summary>
		/// Returns all meetings in existance
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetMeetings()
		{
			return Ok(db.Meetings);
		}

		// GET: odata/Meetings(5)
		/// <summary>
		/// Returns a meeting given the guid
		/// </summary>
		/// <param name="meetingKey"></param>
		/// <returns></returns>
		[HttpGet]
		[ODataRoute("({meetingKey})")]
		[EnableQuery(MaxAnyAllExpressionDepth = 2)]
		public IHttpActionResult GetMeeting([FromODataUri] Guid meetingKey)
		{
			return Ok(SingleResult.Create(db.Meetings.Where(meeting => meeting.MeetingId == meetingKey)));
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				db.Dispose();
			}
			base.Dispose(disposing);
		}

		private bool MeetingExists(Guid key)
		{
			return db.Meetings.Count(e => e.MeetingId == key) > 0;
		}
	}
}
