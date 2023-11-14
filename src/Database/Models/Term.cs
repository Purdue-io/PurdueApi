using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PurdueIo.Database.Models
{
    public class Term
    {
        public Guid Id { get; set; }

        // Short term code, e.g. "202210"
        [StringLength(16)]
        public string Code { get; set; }

        // Friendly name for a term, e.g. "Fall 2021"
        public string Name { get; set; }

        // The date on which the term starts
        // This may not be populated if we haven't determined the first/last meeting time
        public DateOnly? StartDate { get; set; }

        // The date on which the term ends
        // This may not be populated if we haven't determined the first/last meeting time
        public DateOnly? EndDate { get; set; }

        // The classes that occur in this term
        public virtual ICollection<Class> Classes { get; set; }
    }
}