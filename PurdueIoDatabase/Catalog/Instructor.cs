using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIoDb.Catalog
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
		[Key]
		public Guid InstructorId { get; set; }

        /// <summary>
        /// Cluster ID for this entity, for query performance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index(IsClustered = true, IsUnique = true)]
        public int InstructorClusterId { get; set; }

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
		[InverseProperty("Instructors")]
		public virtual ICollection<Meeting> Meetings { get; set; }

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
        /// <summary>
        /// GUID with which to reference an instructor internally.
        /// </summary>
		public Guid InstructorId { get; set; }
        /// <summary>
        /// Instructor's full name as listed in MyPurdue.
        /// </summary>
		public string Name { get; set; }
        /// <summary>
        /// Instructor's e-mail address.
        /// </summary>
		public string Email { get; set; }
	}
}