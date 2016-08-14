﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIoDb.Catalog
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
		[Key]
		public Guid ClassId { get; set; }

        /// <summary>
        /// Cluster ID for this entity, for query performance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index(IsClustered = true, IsUnique = true)]
        public int ClassClusterId { get; set; }

        /// <summary>
        /// ID of the course to which this class belongs.
        /// </summary>
        public Guid CourseId { get; set; }

		/// <summary>
		/// The course to which this class belongs.
		/// </summary>
        [ForeignKey("CourseId")]
		[InverseProperty("Classes")]
		public virtual Course Course { get; set; }

        /// <summary>
        /// ID of the term that this class belongs to.
        /// </summary>
        public Guid TermId { get; set; }

		/// <summary>
		/// The term (e.g. semester) that this class belongs to.
		/// </summary>
        [ForeignKey("TermId")]
		[InverseProperty("Classes")]
		public virtual Term Term { get; set; }

        /// <summary>
        /// ID of the campus on which this class is taught.
        /// </summary>
        public Guid CampusId { get; set; }

		/// <summary>
		/// The campus on which this class is taught.
		/// </summary>
        [ForeignKey("CampusId")]
		public virtual Campus Campus { get; set; }

		/// <summary>
		/// The sections that belong to this class.
		/// </summary>
		[InverseProperty("Class")]
		public virtual ICollection<Section> Sections { get; set; }

		public ClassViewModel ToViewModel()
		{
			return new ClassViewModel()
			{
				ClassId = this.ClassId,
				Course = this.Course.ToViewModel(),
				Term = this.Term.ToViewModel(),
				Campus = this.Campus.ToViewModel()
			};
		}
	}

	/// <summary>
	/// ViewModel for the Class model.
	/// </summary>
	public class ClassViewModel
	{
        /// <summary>
        /// GUID with which to reference this class internally.
        /// </summary>
		public Guid ClassId { get; set; }
        /// <summary>
        /// Id referring to the course this class is part of.
        /// </summary>
		public CourseViewModel Course { get; set; }
        /// <summary>
        /// Object containing information about the term this course is offered in.
        /// </summary>
		public TermViewModel Term { get; set; }
        /// <summary>
        /// Object containing information about the campus this course is located at.
        /// </summary>
		public CampusViewModel Campus { get; set; }
	}
}