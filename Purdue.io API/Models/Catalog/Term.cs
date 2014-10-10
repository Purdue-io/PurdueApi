using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIo.Models.Catalog
{
	/// <summary>
	/// Term model, representing each "semester"-like block of time.
	/// Terms can contain classes, but courses are perpetual - they never change.
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Term
	{
		/// <summary>
		/// Unique ID with which to reference the term internally.
		/// </summary>
		[Key]
		public Guid TermId { get; set; }

		/// <summary>
		/// Short term code such as "fall14" referred to by course catalog.
		/// </summary>
		[Index]
		[StringLength(12)]
		public string TermCode { get; set; }

		/// <summary>
		/// The date on which the term starts.
		/// </summary>
		public DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// The date on which the term ends.
		/// </summary>
		public DateTimeOffset EndDate { get; set; }

		/// <summary>
		/// Queryable list of classes that occur in this term.
		/// </summary>
		[InverseProperty("Term")]
		public virtual ICollection<Class> Classes { get; set; }

		public TermViewModel ToViewModel()
		{
			return new TermViewModel()
			{
				TermId = this.TermId,
				TermCode = this.TermCode,
				StartDate = this.StartDate,
				EndDate = this.EndDate
			};
		}
	}

	/// <summary>
	/// ViewModel for the Term model.
	/// </summary>
	public class TermViewModel
	{
        /// <summary>
        /// GUID with which to reference the term internally.
        /// </summary>
		public Guid TermId { get; set; }
        /// <summary>
        /// Short term code such as "Fall14" referred to by course catalog.
        /// </summary>
		public string TermCode { get; set; }
        /// <summary>
        /// The date on which the term starts.
        /// </summary>
		public DateTimeOffset StartDate { get; set; }
        /// <summary>
        /// The date on which the term ends.
        /// </summary>
		public DateTimeOffset EndDate { get; set; }
	}
}