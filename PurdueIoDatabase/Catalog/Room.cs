using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIoDb.Catalog
{
	/// <summary>
	/// Room model, representing a room in a building where sections can take place.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Room
	{
		/// <summary>
		/// A unique ID with which to refer to this room internally.
		/// </summary>
		[Key]
		public Guid RoomId { get; set; }

        /// <summary>
        /// Cluster ID for this entity, for query performance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index(IsClustered = true, IsUnique = true)]
        public int RoomClusterId { get; set; }

        /// <summary>
        /// The room number. e.g. B156, 1142.
        /// </summary>
        public string Number { get; set; }

        /// <summary>
        /// ID of the building to which this room belongs.
        /// </summary>
        public Guid BuildingId { get; set; }

		/// <summary>
		/// The building to which this room belongs.
		/// </summary>
        [ForeignKey("BuildingId")]
		[InverseProperty("Rooms")]
		public virtual Building Building { get; set; }

		/// <summary>
		/// Queryable list of class sections that take place in this room.
		/// </summary>
		[InverseProperty("Room")]
		public virtual ICollection<Meeting> Meetings { get; set; }

		public RoomViewModel ToViewModel() {
			return new RoomViewModel()
			{
				RoomId = this.RoomId,
				Number = this.Number,
				Building = this.Building.ToViewModel()
			};
		}
	}

	/// <summary>
	/// ViewModel for Room model.
	/// </summary>
	public class RoomViewModel
	{
        /// <summary>
        /// GUID with which to refer to this room internally.
        /// </summary>
		public Guid RoomId { get; set; }
        /// <summary>
        /// The room number.
        /// </summary>
		public string Number { get; set; }
        /// <summary>
        /// Object containing information about the building this room is located in.
        /// </summary>
		public BuildingViewModel Building { get; set; }
	}
}