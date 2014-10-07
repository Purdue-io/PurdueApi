using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
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
		public Guid BuildingId { get; set; }

		/// <summary>
		/// The campus on which this building is located.
		/// </summary>
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
		public Guid BuildingId { get; set; }
		public CampusViewModel Campus { get; set; }
		public string Name { get; set; }
		public string ShortCode { get; set; }
	}
}