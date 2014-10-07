using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
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
		public Guid RoomId { get; set; }

		/// <summary>
		/// The room number. e.g. B156, 1142.
		/// </summary>
		public string Number { get; set; }

		/// <summary>
		/// The building to which this room belongs.
		/// </summary>
		public virtual Building Building { get; set; }

		/// <summary>
		/// Queryable list of class sections that take place in this room.
		/// </summary>
		[InverseProperty("Room")]
		public virtual ICollection<Section> Sections { get; set; }
	}
}