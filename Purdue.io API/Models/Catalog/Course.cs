using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Purdue.io_API.Models.Catalog
{
	/// <summary>
	/// Course model, representing the highest order of class representation.
	/// Contains classes, which contain sections (CRNs).
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Course
	{
		/// <summary>
		/// GUID to reference this course internally.
		/// </summary>
		public Guid CourseId { get; set; }

		/// <summary>
		/// 6 digit code used to identify this course inside of a subject.
		/// e.g. '26500' would be the code representing MA26500. 
		/// </summary>
		[Index]
		[StringLength(8)]
		public string Number { get; set; }

		/// <summary>
		/// Reference to the subject to which this course belongs.
		/// e.g. MA or CS subjects.
		/// </summary>
		public virtual Subject Subject { get; set; }

		/// <summary>
		/// The title of this particular course.
		/// e.g. 'Intro To Computers'
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The number of credit hours offered by taking this course.
		/// </summary>
		public int CreditHours { get; set; }

		/// <summary>
		/// Short blurb describing the course.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Queryable list of classes that belong to this course.
		/// </summary>
		public virtual IQueryable<Class> Classes { get; set; }
	}
}