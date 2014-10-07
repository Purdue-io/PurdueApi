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
	public class Instructor
	{
		/// <summary>
		/// Unique ID with which to reference an instructor internally.
		/// </summary>
		public Guid InstructorId { get; set; }

		/// <summary>
		/// Instructor's full name as listed in MyPurdue.
		/// TODO: Additional parsing for first, last, etc.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Instructor's e-mail address
		/// </summary>
		[Index]
		[StringLength(254)]
		public string Email { get; set; }

		/// <summary>
		/// The sections that this instructor is teaching.
		/// </summary>
		[InverseProperty("Instructor")]
		public virtual ICollection<Section> Sections { get; set; }

		public InstructorViewModel ToViewModel()
		{
			return new InstructorViewModel()
			{
				InstructorId = this.InstructorId,
				Name = this.Name,
				Email = this.Email
			};
		}
	}

	/// <summary>
	/// ViewModel for Instructor model.
	/// </summary>
	public class InstructorViewModel
	{
		public Guid InstructorId { get; set; }
		public string Name { get; set; }
		public string Email { get; set; }
	}
}