using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
{
	/// <summary>
	/// Enum value used to represent section registration status / availability.
	/// </summary>
	public enum RegistrationStatus
	{
		NotAvailable,
		Closed,
		Open
	}

	/// <summary>
	/// Section model, representing the most granular order of class representation.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Section
	{
		/// <summary>
		/// Unique ID with which to identify a section internally.
		/// </summary>
		public Guid SectionId { get; set; }

		/// <summary>
		/// Reference number used by Purdue to refer to sections.
		/// </summary>
		[Index]
		[StringLength(10)]
		public string CRN { get; set; }

		/// <summary>
		/// The class to which this section belongs.
		/// </summary>
		[InverseProperty("Sections")]
		public virtual Class Class { get; set; }

		/// <summary>
		/// The meetings that this section requires (time, place, instructor, etc.).
		/// </summary>
		[InverseProperty("Section")]
		public virtual ICollection<Meeting> Meetings { get; set; }

		/// <summary>
		/// Type of section. e.g. Lecture, Laboratory, etc.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Indicates whether this section is available for registration.
		/// </summary>
		public RegistrationStatus RegistrationStatus { get; set; }
		
		/// <summary>
		/// Date this section begins (earliest meeting date).
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date this section ends (latest meeting date).
		/// </summary>
		public DateTimeOffset EndDate { get; set; }

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

		// TODO: Requisites. These have complicated rules (AND/OR).
		//public virtual ICollection<Course> Requisites { get; set; }

		public SectionViewModel ToViewModel()
		{
			return new SectionViewModel()
			{
				SectionId = this.SectionId,
				CRN = this.CRN,
				Class = this.Class.ToViewModel(),
				Meetings = this.Meetings.ToList().Select(m => m.ToViewModel()).ToList(),
				RegistrationStatus = (int)this.RegistrationStatus,
				Type = this.Type,
				StartDate = this.StartDate,
				EndDate = this.EndDate,
				Capacity = this.Capacity,
				Enrolled = this.Enrolled,
				RemainingSpace = this.RemainingSpace,
				WaitlistCapacity = this.WaitlistCapacity,
				WaitlistCount = this.WaitlistCount,
				WaitlistSpace = this.WaitlistSpace
			};
		}
	}

	/// <summary>
	/// ViewModel for Section model.
	/// </summary>
	public class SectionViewModel
	{
        /// <summary>
        /// GUID with which to identify this section internally.
        /// </summary>
		public Guid SectionId { get; set; }

        /// <summary>
        /// Reference number used by Purdue to refer to sections.
        /// </summary>
		public string CRN { get; set; }

        /// <summary>
        /// Id referring to the class this section is part of.
        /// </summary>
		public ClassViewModel Class { get; set; }

		/// <summary>
		/// The meetings that this section requires (time, place, instructor, etc.).
		/// </summary>
		public virtual ICollection<MeetingViewModel> Meetings { get; set; }

        /// <summary>
        /// Indicates whether this section is available for registration.
        /// </summary>
		public int RegistrationStatus { get; set; }

        /// <summary>
        /// Type of section. e.g. Lecture, Laboratory, etc.
        /// </summary>
		public string Type { get; set; }

        /// <summary>
        /// Date this section begins (earliest meeting date).
        /// </summary>
		public DateTimeOffset StartDate { get; set; }

        /// <summary>
        /// Date this section ends (latest meeting date).
        /// </summary>
		public DateTimeOffset EndDate { get; set; }

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