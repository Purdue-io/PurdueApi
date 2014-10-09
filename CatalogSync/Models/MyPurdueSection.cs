using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogSync.Models
{
	class MyPurdueSection
	{
		public MyPurdueSection()
		{
			this.Meetings = new List<MyPurdueMeeting>();
		}
		/// <summary>
		/// Course CRN number. e.g., 68475.
		/// </summary>
		public string Crn { get; set; }

		/// <summary>
		/// List of meetings that this section has.
		/// </summary>
		public ICollection<MyPurdueMeeting> Meetings { get; set; }

		/// <summary>
		/// Subject code of the course. e.g., CS.
		/// </summary>
		public string SubjectCode { get; set; }

		/// <summary>
		/// Number of the course. e.g., 11000.
		/// </summary>
		public string Number { get; set; }

		/// <summary>
		/// Type of section. e.g., Lecture.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Title of the course. e.g., Introduction to Computers.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Description of the course.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Number of credit hours gained by taking this section.
		/// </summary>
		public double CreditHours { get; set; }

		/// <summary>
		/// Link ID of this section. e.g., A2.
		/// </summary>
		public string LinkSelf { get; set; }

		/// <summary>
		/// Link ID of other section. e.g., A1.
		/// </summary>
		public string LinkOther { get; set; }

		/// <summary>
		/// The name of the campus this section meets on.
		/// </summary>
		public string CampusName { get; set; }
	}
}
