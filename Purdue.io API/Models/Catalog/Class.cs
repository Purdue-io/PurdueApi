using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
{
	/// <summary>
	/// Class model, representing the second order of class representation.
	/// Part of Contains classes, which contain sections (CRNs).
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Class
	{
		/// <summary>
		/// Unique ID with which to reference a class internally.
		/// </summary>
		public Guid ClassId { get; set; }

		/// <summary>
		/// The course to which this class belongs.
		/// </summary>
		public virtual Course Course { get; set; }
	}
}