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

        // Date this section begins (earliest meeting date)
        public DateOnly? StartDate { get; set; }

        // Date this section ends (latest meeting date)
        public DateOnly? EndDate { get; set; }
    }
}