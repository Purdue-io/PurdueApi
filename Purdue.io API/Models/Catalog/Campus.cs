using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
{
	/// <summary>
	/// Campus model, representing physical location of a school. Collection of buildings.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Campus
	{
		/// <summary>
		/// Unique ID with which to reference this campus internally.
		/// </summary>
		[Key]
		public Guid CampusId { get; set; }

		/// <summary>
		/// Friendly name of this campus. e.g. "Purdue University West Lafayette".
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Zip Code of the campus location.
		/// </summary>
		[StringLength(5)]
		public string ZipCode { get; set; }

		/// <summary>
		/// Buildings that belong on this campus.
		/// </summary>
		[InverseProperty("Campus")]
		public virtual ICollection<Building> Buildings { get; set; }

		public CampusViewModel ToViewModel()
		{
			return new CampusViewModel()
			{
				CampusId = this.CampusId,
				Name = this.Name,
				ZipCode = this.ZipCode
			};
		}
	}

	/// <summary>
	/// ViewModel for the Campus model.
	/// </summary>
	public class CampusViewModel
	{
        /// <summary>
        /// GUID with which to reference this campus internally.
        /// </summary>
		public Guid CampusId { get; set; }
        /// <summary>
        /// Official name of this campus.
        /// </summary>
		public string Name { get; set; }
        /// <summary>
        /// Zip Code of the campus location.
        /// </summary>
		public string ZipCode { get; set; }
	}
}