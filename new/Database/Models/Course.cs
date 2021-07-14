using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PurdueIo.Database.Models
{
    public class Course
    {
        public Guid Id { get; set; }

        // 6 digit code used to identify this course inside of a subject, e.g. "26500" for "MA26500"
        [StringLength(16)]
        public string Number { get; set; }

        // ID of the subject this course belongs to
        public Guid SubjectId { get; set; }

        // Subject this course belongs to
        public virtual Subject Subject { get; set; }

        // Title of the course, e.g. "Intro to Computers"
        public string Title { get; set; }

        // Number of credit hours earned by taking this course
        public double CreditHours { get; set; }

        // Short blurb describing the course
        public string Description { get; set; }

        // Classes that belong to this course
        public virtual ICollection<Class> Classes { get; set; }
    }
}