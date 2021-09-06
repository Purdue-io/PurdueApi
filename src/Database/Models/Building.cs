using System;
using System.Collections.Generic;

namespace PurdueIo.Database.Models
{
    public class Building
    {
        public Guid Id { get; set; }

        // ID of the campus this building belongs to
        public Guid CampusId { get; set; }

        // The campus this building belongs to
        public virtual Campus Campus { get; set; }

        // The friendly name of this building, e.g. "Lawson Computer Science Building"
        public string Name { get; set; }

        // The shortened code of this building, e.g. "LWSN"
        public string ShortCode { get; set; }

        // List of rooms in this building
        public virtual ICollection<Room> Rooms { get; set; }
    }
}