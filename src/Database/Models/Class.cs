using System;
using System.Collections.Generic;

namespace PurdueIo.Database.Models
{
    public class Class
    {
        public Guid Id { get; set; }

        // ID of the course this class belongs to
        public Guid CourseId { get; set; }

        // The course this class belongs to
        public virtual Course Course { get; set; }

        // ID of the term this class belongs to
        public Guid TermId { get; set; }

        // The term this class belongs to
        public virtual Term Term { get; set; }

        // ID of the campus this class is taught on
        public Guid CampusId { get; set; }

        // Campus this class is taught on
        public virtual Campus Campus { get; set; }

        // The sections that belong to this class
        public virtual ICollection<Section> Sections { get; set; }
    }
}