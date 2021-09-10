using System;

namespace PurdueIo.Database.Models
{
    public record MeetingInstructor
    {
        // ID of the meeting taught by the instructor
        public Guid MeetingId { get; init; }

        // Meeting taught by the instructor
        public virtual Meeting Meeting { get; init; }

        // ID of the instructor teaching the meeting
        public Guid InstructorId { get; init; }

        // Instructor teaching the meeting
        public virtual Instructor Instructor { get; init; }
    }
}