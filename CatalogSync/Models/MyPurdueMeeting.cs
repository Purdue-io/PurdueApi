using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogSync.Models
{
	public class MyPurdueMeeting
	{
		public MyPurdueMeeting()
		{
			this.DaysOfWeek = new List<DayOfWeek>();
			this.Instructors = new List<Tuple<string, string>>();
		}
		/// <summary>
		/// Type of meeting. e.g. Lecture, Laboratory, etc.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// List of instructor tuples, in format [name, email].
		/// </summary>
		public List<Tuple<string, string>> Instructors { get; set; }

		/// <summary>
		/// Date that this class begins.
		/// </summary>
		public DateTime StartDate { get; set; }

		/// <summary>
		/// Date that this class ends.
		/// </summary>
		public DateTime EndDate { get; set; }

		/// <summary>
		/// The days of the week that this class meets.
		/// </summary>
		public List<DayOfWeek> DaysOfWeek { get; set; }

		/// <summary>
		/// The time in the day that this class begins.
		/// </summary>
		public DateTimeOffset StartTime { get; set; }

		/// <summary>
		/// The time int he day that this class ends.
		/// </summary>
		public DateTimeOffset EndTime { get; set; }

		/// <summary>
		/// The short name of the building this section meets in.
		/// </summary>
		public string BuildingCode { get; set; }

		/// <summary>
		/// Name of the building this section meets in.
		/// </summary>
		public string BuildingName { get; set; }

		/// <summary>
		/// Number of room that this section meets in.
		/// </summary>
		public string RoomNumber { get; set; }
	}
}
