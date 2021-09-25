using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PurdueIo.Database.Models
{
    public class Subject
    {
        public Guid Id { get; set; }

        // The full name of the subject, e.g. "Computer Science".
        public string Name { get; set; }

        // The 2-4 character abbrevation of the subject, e.g. "CS" or "ENG".
        [StringLength(6)]
        public string Abbreviation { get; set; }

        // The courses that belong to this subject.
        public virtual ICollection<Course> Courses { get; set; }
    }
}