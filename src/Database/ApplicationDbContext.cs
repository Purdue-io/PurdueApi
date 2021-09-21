using PurdueIo.Database.Models;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PurdueIo.Database
{
    public class ApplicationDbContext : DbContext
    {
        /// All school campuses in course catalog.
        public DbSet<Campus> Campuses { get; set; }

        /// All campus buildings in course catalog.
        public DbSet<Building> Buildings { get; set; }

        /// All building rooms in course catalog.
        public DbSet<Room> Rooms { get; set; }

        /// All terms in the catalog.
        public DbSet<Term> Terms { get; set; }

        /// All courses in the catalog - superset of classes which define a curriculum.
        public DbSet<Course> Courses { get; set; }

        /// All classes in the catalog - a subset of courses that have a specific group of sections
        public DbSet<Class> Classes { get; set; }

        /// All sections in the catalog - a subset of classes.
        public DbSet<Section> Sections { get; set; }

        /// All meetings in the catalog - a specific time, place, and instructor. Part of a class.
        public DbSet<Meeting> Meetings { get; set; }

        /// All subjects in the catalog. Each course belongs to a subject.
        public DbSet<Subject> Subjects { get; set; }

        /// All instructors in the catalog.
        public DbSet<Instructor> Instructors { get; set; }

        public ApplicationDbContext([NotNullAttribute] DbContextOptions options) : 
            base(options)
        {
            Database.Migrate();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Campus>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<Campus>()
                .HasIndex(c => c.Code)
                .IsUnique();
            modelBuilder.Entity<Campus>()
                .HasIndex(c => c.Name)
                .IsUnique();
            modelBuilder.Entity<Campus>()
                .HasMany<Building>(c => c.Buildings)
                .WithOne(b => b.Campus)
                .HasForeignKey(b => b.CampusId);
            modelBuilder.Entity<Campus>()
                .HasMany<Class>(c => c.Classes)
                .WithOne(c => c.Campus)
                .HasForeignKey(c => c.CampusId);

            modelBuilder.Entity<Building>()
                .HasKey(b => b.Id);
            modelBuilder.Entity<Building>()
                .HasIndex(b => b.Name);
            modelBuilder.Entity<Building>()
                .HasIndex(b => b.ShortCode);
            modelBuilder.Entity<Building>()
                .HasIndex(b => new { b.CampusId, b.ShortCode })
                .IsUnique();
            modelBuilder.Entity<Building>()
                .HasMany<Room>(b => b.Rooms)
                .WithOne(r => r.Building)
                .HasForeignKey(r => r.BuildingId);

            modelBuilder.Entity<Room>()
                .HasKey(r => r.Id);
            modelBuilder.Entity<Room>()
                .HasIndex(r => r.Number);

            modelBuilder.Entity<Term>()
                .HasKey(t => t.Id);
            modelBuilder.Entity<Term>()
                .HasIndex(t => t.Code)
                .IsUnique();
            modelBuilder.Entity<Term>()
                .HasIndex(t => t.Name)
                .IsUnique();
            modelBuilder.Entity<Term>()
                .HasMany<Class>(t => t.Classes)
                .WithOne(c => c.Term)
                .HasForeignKey(c => c.TermId);

            modelBuilder.Entity<Course>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<Course>()
                .HasIndex(c => c.Number);
            modelBuilder.Entity<Course>()
                .HasIndex(c => c.Title);
            modelBuilder.Entity<Course>()
                .HasMany<Class>(c => c.Classes)
                .WithOne(c => c.Course)
                .HasForeignKey(c => c.CourseId);

            modelBuilder.Entity<Class>()
                .HasKey(c => c.Id);
            modelBuilder.Entity<Class>()
                .HasMany<Section>(c => c.Sections)
                .WithOne(s => s.Class)
                .HasForeignKey(s => s.ClassId);

            modelBuilder.Entity<Section>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Section>()
                .HasIndex(s => s.Crn);
            modelBuilder.Entity<Section>()
                .HasMany<Meeting>(s => s.Meetings)
                .WithOne(m => m.Section)
                .HasForeignKey(m => m.SectionId);

            modelBuilder.Entity<Meeting>()
                .HasKey(m => m.Id);
            modelBuilder.Entity<Meeting>()
                .HasMany<Instructor>(m => m.Instructors)
                .WithMany(i => i.Meetings)
                .UsingEntity<MeetingInstructor>(
                    mi => mi.HasOne(mii => mii.Instructor).WithMany()
                        .HasForeignKey(mii => mii.InstructorId),
                    mi => mi.HasOne(mii => mii.Meeting).WithMany()
                        .HasForeignKey(mii => mii.MeetingId))
                .HasKey(mi => new { mi.MeetingId, mi.InstructorId });
            modelBuilder.Entity<Meeting>()
                .HasOne<Room>(m => m.Room)
                .WithMany()
                .HasForeignKey(m => m.RoomId);

            modelBuilder.Entity<Subject>()
                .HasKey(s => s.Id);
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Abbreviation)
                .IsUnique();
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.Name);
            modelBuilder.Entity<Subject>()
                .HasMany<Course>(s => s.Courses)
                .WithOne(c => c.Subject)
                .HasForeignKey(c => c.SubjectId);

            modelBuilder.Entity<Instructor>()
                .HasKey(i => i.Id);
            modelBuilder.Entity<Instructor>()
                .HasIndex(i => i.Name);
            modelBuilder.Entity<Instructor>()
                .HasIndex(i => i.Email);

            // SQLite does not have proper support for DateTimeOffset via Entity Framework Core:
            // https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations
            //
            // To work around this, when the Sqlite database provider is used, all model properties
            // of type DateTimeOffset use the DateTimeOffsetToBinaryConverter based on:
            // https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
            //
            // NOTE: This only supports millisecond precision, and datetimes across different zones
            // are not sorted correctly.
            //
            // Thanks Georg Dangl for this workaround
            // @ https://blog.dangl.me/archive/handling-datetimeoffset-in-sqlite-with-entity-framework-core/
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in modelBuilder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties()
                        .Where(p => p.PropertyType == typeof(DateTimeOffset));
                    foreach (var property in properties)
                    {
                        modelBuilder
                            .Entity(entityType.Name)
                            .Property(property.Name)
                            .HasConversion(new DateTimeOffsetToBinaryConverter());
                    }
                }
            }
        }
    }
}