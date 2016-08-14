using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIoDb.Catalog
{
	/// <summary>
	/// Building model, representing collections of rooms where sections can take place.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Building
	{
		/// <summary>
		/// Unique ID with which to reference this building internally.
		/// </summary>
		[Key]
		public Guid BuildingId { get; set; }

        /// <summary>
        /// Cluster ID for this entity, for query performance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index(IsClustered = true, IsUnique = true)]
        public int BuildingClusterId { get; set; }

        /// <summary>
        /// ID of the campus on which this building is located.
        /// </summary>
        public Guid CampusId { get; set; }

		/// <summary>
		/// The campus on which this building is located.
		/// </summary>
        [ForeignKey("CampusId")]
		[InverseProperty("Buildings")]
		public virtual Campus Campus { get; set; }

		/// <summary>
		/// The friendly name of this building, e.g. "Lawson Computer Science Building".
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The shortened code name of this building, e.g. "LWSN".
		/// </summary>
		[Index]
		[StringLength(8)]
		public string ShortCode { get; set; }

		/// <summary>
		/// Queryable list of rooms in this building.
		/// </summary>
		[InverseProperty("Building")]
		public virtual ICollection<Room> Rooms { get; set; }

		public BuildingViewModel ToViewModel()
		{
			return new BuildingViewModel()
			{
				BuildingId = this.BuildingId,
				Campus = this.Campus.ToViewModel(),
				Name = this.Name,
				ShortCode = this.ShortCode
			};
		}
	}

	/// <summary>
	/// ViewModel for the Building model.
	/// </summary>
	public class BuildingViewModel
	{
        /// <summary>
        /// GUID with which to reference this building internally.
        /// </summary>
		public Guid BuildingId { get; set; }
        /// <summary>
        /// Object containing information about the campus this building is located at.
        /// </summary>
		public CampusViewModel Campus { get; set; }
        /// <summary>
        /// The official name of this building
        /// </summary>
		public string Name { get; set; }
        /// <summary>
        /// The shortened code name of this building.
        /// </summary>
		public string ShortCode { get; set; }
	}
}