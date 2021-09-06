using System;

namespace PurdueIo.Database.Models
{
    public class Room
    {
        public Guid Id { get; set; }

        // The room number, e.g. B156
        public string Number { get; set; }

        // ID of the building this room belongs to
        public Guid BuildingId { get; set; }

        // Building this room belongs to
        public virtual Building Building { get; set; }
    }
}