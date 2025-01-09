using System;

namespace PurdueIo.Database.Models
{
    public record ClassSection
    {
        public Guid ClassId { get; init; }
        public virtual Class Class { get; init; }

        public Guid SectionId { get; init; }
        public virtual Section Section { get; init; }
    }
}