using Microsoft.AspNet.Identity.EntityFramework;
using PurdueIo.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace PurdueIo.Models
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext()
			: base("DefaultConnection", throwIfV1Schema: false)
		{
		}

		public static ApplicationDbContext Create()
		{
			return new ApplicationDbContext();
		}

		/// <summary>
		/// All school campuses in course catalog.
		/// </summary>
		public DbSet<Campus> Campuses { get; set; }

		/// <summary>
		/// All campus buildings in course catalog.
		/// </summary>
		public DbSet<Building> Buildings { get; set; }

		/// <summary>
		/// All building rooms in course catalog.
		/// </summary>
		public DbSet<Room> Rooms { get; set; }

		/// <summary>
		/// All terms in the catalog.
		/// </summary>
		public DbSet<Term> Terms { get; set; }

		/// <summary>
		/// All courses in the catalog - superset of classes which define a curriculum.
		/// </summary>
		public DbSet<Course> Courses { get; set; }

		/// <summary>
		/// All classes in the catalog - a subset of courses that have a specific group of sections
		/// </summary>
		public DbSet<Class> Classes { get; set; }

		/// <summary>
		/// All sections in the catalog - a specific time, place, and instructor. Part of a class.
		/// </summary>
		public DbSet<Section> Sections { get; set; }

		/// <summary>
		/// All subjects in the catalog. Each course belongs to a subject.
		/// </summary>
		public DbSet<Subject> Subjects { get; set; }

		/// <summary>
		/// All instructors in the catalog.
		/// </summary>
		public DbSet<Instructor> Instructors { get; set; }
	}
}