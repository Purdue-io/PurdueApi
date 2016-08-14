using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIoDb.Catalog
{
	[Flags]
	public enum DOW : byte
	{
		Monday = 1,
		Tuesday = 2,
		Wednesday = 4,
		Thursday = 8,
		Friday = 16,
		Saturday = 32,
		Sunday = 64
	}

	public class Meeting
	{
		/// <summary>
		/// Unique ID with which to identify a meeting internally.
		/// </summary>
		[Key]
		public Guid MeetingId { get; set; }

        /// <summary>
        /// Cluster ID for this entity, for query performance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index(IsClustered = true, IsUnique = true)]
        public int MeetingClusterId { get; set; }

        /// <summary>
        /// ID of the section this meeting is a part of.
        /// </summary>
        public Guid SectionId { get; set; }

		/// <summary>
		/// The section this meeting is a part of.
		/// </summary>
        [ForeignKey("SectionId")]
		[InverseProperty("Meetings")]
		public virtual Section Section { get; set; }

		/// <summary>
		/// Type of meeting. e.g. Lecture, Laboratory, etc.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// The instructors who teach this meeting.
		/// </summary>
		[InverseProperty("Meetings")]
		public virtual ICollection<Instructor> Instructors { get; set; }

		/// <summary>
		/// Date this meeting begins.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date this meeting ends.
		/// </summary>
		public DateTimeOffset EndDate { get; set; }

		/// <summary>
		/// Days of the week on which this meeting occurs.
		/// </summary>
		public DOW DaysOfWeek { get; set; }

		/// <summary>
		/// The time this meeting occurs.
		/// </summary>
		public DateTimeOffset StartTime { get; set; }

		/// <summary>
		/// The time duration for which this meeting occurs.
		/// </summary>
		public TimeSpan Duration { get; set; }

        /// <summary>
        /// ID of the room in which this meeting occurs.
        /// </summary>
        public Guid? RoomId { get; set; }

		/// <summary>
		/// The room in which this meeting occurs.
		/// </summary>
        [ForeignKey("RoomId")]
		[InverseProperty("Meetings")]
		public virtual Room Room { get; set; }

		public MeetingViewModel ToViewModel()
		{
			return new MeetingViewModel()
			{
				MeetingId = this.MeetingId,
				SectionId = this.Section.SectionId,
				Type = this.Type,
				Instructors = this.Instructors.ToList().Select(i => i.ToViewModel()).ToList(),
				StartDate = this.StartDate,
				EndDate = this.EndDate,
				DaysOfWeek = this.DaysOfWeek,
				StartTime = this.StartTime,
				Duration = this.Duration
			};
		}
	}

	public class MeetingViewModel
	{
		/// <summary>
		/// Unique ID with which to identify a meeting internally.
		/// </summary>
		public Guid MeetingId { get; set; }

		/// <summary>
		/// The section id referring to the section this meeting is a part of.
		/// </summary>
		public Guid SectionId { get; set; }

		/// <summary>
		/// Type of meeting. e.g. Lecture, Laboratory, etc.
		/// </summary>
		public string Type { get; set; }

		/// <summary>
		/// The instructors who teach this meeting.
		/// </summary>
		public ICollection<InstructorViewModel> Instructors { get; set; }

		/// <summary>
		/// Date this meeting begins.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date this meeting ends.
		/// </summary>
		public DateTimeOffset EndDate { get; set; }

		/// <summary>
		/// Days of the week on which this meeting occurs.
		/// </summary>
		public DOW DaysOfWeek { get; set; }

		/// <summary>
		/// The time this meeting occurs.
		/// </summary>
		public DateTimeOffset StartTime { get; set; }

		/// <summary>
		/// The time duration for which this meeting occurs.
		/// </summary>
		public TimeSpan Duration { get; set; }
	}
}