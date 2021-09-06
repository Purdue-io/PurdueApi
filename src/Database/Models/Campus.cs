using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PurdueIo.Database.Models
{
    public class Campus
    {
        public Guid Id { get; set; }

        // A short code to refer to this campus. e.g. "PWL"
        [StringLength(12)]
        public string Code { get; set; }

        // Friendly name of this campus. e.g. "Purdue University West Lafayette"
        public string Name { get; set; }

        // Zip code of the campus location.
        [StringLength(5)]
        public string ZipCode { get; set; }

        // Buildings that belong to this campus
        public virtual ICollection<Building> Buildings { get; set; }

        // The classes taught on this campus
        public virtual ICollection<Class> Classes { get; set; }
    }
}