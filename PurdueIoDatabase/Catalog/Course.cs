using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace PurdueIoDb.Catalog
{
	/// <summary>
	/// Course model, representing the highest order of class representation.
	/// Contains classes, which contain sections (CRNs).
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// !! DO NOT MODIFY THIS CLASS UNLESS YOU ARE FAMILIAR WITH MIGRATIONS !!
	/// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
	/// </summary>
	public class Course
	{
		/// <summary>
		/// GUID to reference this course internally.
		/// </summary>
		[Key]
		public Guid CourseId { get; set; }

        /// <summary>
        /// Cluster ID for this entity, for query performance.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Index(IsClustered = true, IsUnique = true)]
        public int CourseClusterId { get; set; }

        /// <summary>
        /// 6 digit code used to identify this course inside of a subject.
        /// e.g. '26500' would be the code representing MA26500. 
        /// </summary>
        [Index]
		[StringLength(8)]
		public string Number { get; set; }

        /// <summary>
        /// ID of the subject to which this course belongs.
        /// </summary>
        public Guid SubjectId { get; set; }

		/// <summary>
		/// Reference to the subject to which this course belongs.
		/// e.g. MA or CS subjects.
		/// </summary>
        [ForeignKey("SubjectId")]
		[InverseProperty("Courses")]
		public virtual Subject Subject { get; set; }

		/// <summary>
		/// The title of this particular course.
		/// e.g. 'Intro To Computers'
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// The number of credit hours offered by taking this course.
		/// </summary>
		public double CreditHours { get; set; }

		/// <summary>
		/// Short blurb describing the course.
		/// </summary>
		public string Description { get; set; }

		/// <summary>
		/// Queryable list of classes that belong to this course.
		/// </summary>
		[InverseProperty("Course")]
		public virtual ICollection<Class> Classes { get; set; }

		public CourseViewModel ToViewModel()
		{
			return new CourseViewModel()
			{
				CourseId = this.CourseId,
				Number = this.Number,
				Subject = this.Subject != null ? this.Subject.ToViewModel() : null,
				Title = this.Title,
				CreditHours = this.CreditHours,
				Description = this.Description
			};
		}
	}

	/// <summary>
	/// ViewModel for the Course model.
	/// </summary>
	public class CourseViewModel
	{
		/// <summary>
        /// GUID to reference this course internally.
		/// </summary>
        public Guid CourseId { get; set; }
        /// <summary>
        /// Object containing information about the subject this course belongs to.
        /// </summary>
		public SubjectViewModel Subject { get; set; }
		/// <summary>
        /// 6 digit code used to identify this course inside of a subject.
		/// </summary>
        public string Number { get; set; }
        /// <summary>
        /// The title of this particular course.
        /// </summary>
		public string Title { get; set; }
        /// <summary>
        /// The number of credit hours offered by taking this course.
        /// </summary>
		public double CreditHours { get; set; }
        /// <summary>
        /// Short blurb describing the course.
        /// </summary>
		public string Description { get; set; }
	}
}