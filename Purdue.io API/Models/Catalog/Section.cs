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
		/// The instructor who teaches this section.
		/// </summary>
		[InverseProperty("Sections")]
		public virtual ICollection<Instructor> Instructors { get; set; }

		/// <summary>
		/// Indicates whether this section is available for registration.
		/// </summary>
		public RegistrationStatus RegistrationStatus { get; set; }

		/// <summary>
		/// Type of section. e.g. Lecture, Laboratory, etc.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// Date this section begins.
		/// </summary>
		public DateTime StartDate { get; set; }

		/// <summary>
		/// Date this section ends.
		/// </summary>
		public DateTime EndDate { get; set; }

		/// <summary>
		/// Days of the week on which this section occurs.
		/// </summary>
		public ICollection<DayOfWeek> DaysOfWeek { get; set; }

		/// <summary>
		/// The time this section meets
		/// </summary>
		public DateTimeOffset StartTime { get; set; }

		/// <summary>
		/// The time duration for which this section meets.
		/// </summary>
		public TimeSpan Duration { get; set; }

		/// <summary>
		/// The room in which this section meets.
		/// </summary>
		[InverseProperty("Sections")]
		public virtual Room Room { get; set; }

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
				Instructors = this.Instructors.ToList().Select(i => i.ToViewModel()).ToList(),
				RegistrationStatus = (int)this.RegistrationStatus,
				Type = this.Type,
				StartDate = this.StartDate,
				EndDate = this.EndDate,
				//DaysOfWeek = this.DaysOfWeek.Select(x => (int)x).ToList(),
				StartTime = this.StartTime,
				Duration = this.Duration,
				Room = this.Room.ToViewModel(),
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
		public Guid SectionId { get; set; }
		public string CRN { get; set; }
		public ClassViewModel Class { get; set; }
		public ICollection<InstructorViewModel> Instructors { get; set; }
		public int RegistrationStatus { get; set; }
		public string Type { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public ICollection<int> DaysOfWeek { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public TimeSpan Duration { get; set; }
		public RoomViewModel Room { get; set; }
		public int Capacity { get; set; }
		public int Enrolled { get; set; }
		public int RemainingSpace { get; set; }
		public int WaitlistCapacity { get; set; }
		public int WaitlistCount { get; set; }
		public int WaitlistSpace { get; set; }
	}
}