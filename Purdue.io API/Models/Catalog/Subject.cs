using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
{
	/// <summary>
	/// Subject model, representing a subject such as 'CS' or 'MA'.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Subject
	{
		/// <summary>
		/// Unique ID with which to reference a subject internally.
		/// </summary>
		public Guid SubjectId { get; set; }

		/// <summary>
		/// The full name of the subject, e.g. "Computer Science".
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The 2-4 character abbreviation of the subject, e.g. "CS" or "ENG".
		/// </summary>
		[Index]
		[StringLength(5)]
		public string Abbreviation { get; set; }

		/// <summary>
		/// The courses that belong to this subject.
		/// </summary>
		[InverseProperty("Subject")]
		public virtual ICollection<Course> Courses { get; set; }
	}
}