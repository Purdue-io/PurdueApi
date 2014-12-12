using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Binding
{
	public class CourseSearchBindingModel
	{
		/// <summary>
		/// Course Data
		/// Description is omitted, users shouldn't search by description (maybe for a general search?
		/// </summary>
		public Guid CourseId { get; set; }
		public Guid Subject { get; set; }
		public String Number { get; set; }
		public String Title { get; set; }
		public double CreditHours { get; set; }
		public Guid Class { get; set; }
	}
}