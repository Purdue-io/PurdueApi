using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Models
{
	public class MyPurdueSectionSeats
	{
		/// <summary>
		/// Section CRN number. e.g., 68475.
		/// </summary>
		public string Crn { get; set; }

		/// <summary>
		/// Section code (usually three characters, differentiates section)
		/// </summary>
		public string SectionCode { get; set; }

		/// <summary>
		/// Subject code of the course. e.g., CS.
		/// </summary>
		public string SubjectCode { get; set; }

		/// <summary>
		/// Number of the course. e.g., 11000.
		/// </summary>
		public string Number { get; set; }

		/// <summary>
		/// Title of the course. e.g., Introduction to Computers.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// This section's attendance capacity.
		/// </summary>
		public int Capacity { get; set; }

		/// <summary>
		/// How many students are enrolled - referred to as 'Actual' by MyPurdue.
		/// </summary>
		public int Enrolled { get; set; }

		/// <summary>
		/// Remaining space for enrollment.
		/// </summary>
		public int RemainingSpace { get; set; }

		/// <summary>
		/// Wait list capacity.
		/// </summary>
		public int WaitlistCapacity { get; set; }

		/// <summary>
		/// How many students are on the wait list.
		/// </summary>
		public int WaitlistCount { get; set; }

		/// <summary>
		/// How much space is available on the wait list.
		/// </summary>
		public int WaitlistSpace { get; set; }
	}
}
