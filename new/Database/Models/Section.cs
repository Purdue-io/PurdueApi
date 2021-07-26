using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PurdueIo.Database.Models
{
    public class Section
    {
        public Guid Id { get; set; }

        // Class reference number (CRN) used to uniquely identify a section on MyPurdue
        [StringLength(16)]
        public string Crn { get; set; }

        // ID of the class this section belongs to
        public Guid ClassId { get; set; }

        // Class this section belongs to
        public virtual Class Class { get; set; }

        // Meetings scheduled for this section
        public virtual ICollection<Meeting> Meetings { get; set; }

        // Type of section, e.g. "Lecture"
        public string Type { get; set; }

        // Indicates what type of registration is available
        public RegistrationStatus RegistrationStatus { get; set; }

        // Date this section begins (earliest meeting date)
        public DateTimeOffset StartDate { get; set; }

        // Date this section ends (latest meeting date)
        public DateTimeOffset EndDate { get; set; }

        // Max enrollment capacity
        public int Capacity { get; set; }

        // Number of students currently enrolled
        public int Enrolled { get; set; }

        // Remaining enrollment space
        public int RemainingSpace { get; set; }

        // Wait list capacity
        public int WaitListCapacity { get; set; }

        // Number of students currently on the wait list
        public int WaitListCount { get; set; }

        // Number of spaces available on the wait list
        public int WaitListSpace { get; set; }

    }
}