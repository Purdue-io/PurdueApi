using System;
using System.Collections.Generic;

namespace PurdueIo.Database.Models
{
    public class Meeting
    {
        public Guid Id { get; set; }

        // ID of the section this meeting belongs to
        public Guid SectionId { get; set; }

        // The section this meeting belongs to
        public virtual Section Section { get; set; }

        // Type of meeting, e.g. "Lecture"
        public string Type { get; set; }

        // Instructors who will conduct this meeting
        public virtual ICollection<Instructor> Instructors { get; set; }

        // Date of first meeting occurrence
        public DateOnly? StartDate { get; set; }

        // Date of final meeting occurrence
        public DateOnly? EndDate { get; set; }

        // Days of the week this meeting occurs
        public DaysOfWeek DaysOfWeek { get; set; }

        // The time each day this meeting starts, if applicable
        public TimeOnly? StartTime { get; set; }

        // The duration of this meeting
        public TimeSpan Duration { get; set; }

        // ID of the room where this meeting occurs (if any)
        public Guid? RoomId { get; set; }

        // Room where this meeting occurs (if any)
        public virtual Room Room { get; set; }
    }
}