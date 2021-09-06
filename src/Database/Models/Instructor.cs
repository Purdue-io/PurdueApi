using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PurdueIo.Database.Models
{
    public class Instructor
    {
        public Guid Id { get; set; }

        // Instructor's full name as listed in MyPurdue
        public string Name { get; set; }

        // Instructor's e-mail address
        [StringLength(254)]
        public string Email { get; set; }

        // Meetings this instructor is teaching
        public virtual ICollection<Meeting> Meetings { get; set; }
    }
}