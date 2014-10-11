namespace PurdueIo.Migrations
{
	using PurdueIo.Models.Catalog;
	using System;
	using System.Collections.Generic;
	using System.Data.Entity;
	using System.Data.Entity.Migrations;
	using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<PurdueIo.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(PurdueIo.Models.ApplicationDbContext context)
        {
#if DEBUG
			// Construct a basic Purdue University campus.
			var campus = new Campus()
			{
				CampusId = Guid.NewGuid(),
				Name = "Purdue University West Lafayette",
				Buildings = new List<Building>(),
				ZipCode = "47907"
			};

			var lawson = new Building()
			{
				BuildingId = Guid.NewGuid(),
				Campus = campus,
				Name = "Lawson Computer Science Building",
				Rooms = new List<Room>(),
				ShortCode = "LWSN"
			};

			var windowsLab = new Room()
			{
				RoomId = Guid.NewGuid(),
				Building = lawson,
				Number = "B160",
				Meetings = new List<Meeting>()
			};
			lawson.Rooms.Add(windowsLab);

			var linuxLab = new Room()
			{
				RoomId = Guid.NewGuid(),
				Building = lawson,
				Number = "B146",
				Meetings = new List<Meeting>()
			};
			lawson.Rooms.Add(linuxLab);

			campus.Buildings.Add(lawson);

			context.Campuses.Add(campus);
			context.SaveChanges();

			// Construct a course.
			var term = new Term()
			{
				TermId = Guid.NewGuid(),
				TermCode = "201510",
				StartDate = new DateTimeOffset(new DateTime(2014, 8, 25), TimeZoneInfo.Local.BaseUtcOffset),
				EndDate = new DateTimeOffset(new DateTime(2014, 12, 19), TimeZoneInfo.Local.BaseUtcOffset),
				Classes = new List<Class>()
			};

			var cs = new Subject()
			{
				SubjectId = Guid.NewGuid(),
				Name = "Computer Science",
				Abbreviation = "CS",
				Courses = new List<Course>()
			};
			var dunsmore = new Instructor()
			{
				InstructorId = Guid.NewGuid(),
				Name = "Hubert E Dunsmore",
				Email = "bxd@purdue.edu",
				Meetings = new List<Meeting>()
			};

			var course = new Course()
			{
				CourseId = Guid.NewGuid(),
				Title = "Software Engineering",
				Subject = cs,
				Number = "30700",
				CreditHours = 3.000,
				Description = "A pretty cool CS class.",
				Classes = new List<Class>()
			};
			cs.Courses.Add(course);

			var csclass = new Class()
			{
				ClassId = Guid.NewGuid(),
				Campus = campus,
				Course = course,
				Term = term,
				Sections = new List<Section>()
			};
			term.Classes.Add(csclass);
			course.Classes.Add(csclass);

			var section = new Section()
			{
				SectionId = Guid.NewGuid(),
				Class = csclass,
				Type = "Lecture",
				Meetings = new List<Meeting>(),
				StartDate = new DateTimeOffset(new DateTime(2014, 8, 25), TimeZoneInfo.Local.BaseUtcOffset),
				EndDate = new DateTimeOffset(new DateTime(2014, 12, 19), TimeZoneInfo.Local.BaseUtcOffset),
				CRN = "12345",
				Capacity = 30,
				Enrolled = 20,
				RemainingSpace = 10,
				WaitlistCapacity = 0,
				WaitlistCount = 0,
				WaitlistSpace = 0
			};
			section.Meetings.Add(new Meeting()
			{
				MeetingId = Guid.NewGuid(),
				Section = section,
				Instructors = new List<Instructor>() { dunsmore },
				StartDate = new DateTimeOffset(new DateTime(2014, 8, 25), TimeZoneInfo.Local.BaseUtcOffset),
				EndDate = new DateTimeOffset(new DateTime(2014, 12, 19), TimeZoneInfo.Local.BaseUtcOffset),
				DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday },
				StartTime = new DateTimeOffset(2000, 1, 1, 12, 0, 0, TimeZoneInfo.Local.BaseUtcOffset),
				Duration = new TimeSpan(0, 50, 0),
				Room = windowsLab
			});
			csclass.Sections.Add(section);

			context.Courses.Add(course);
			context.SaveChanges();
#endif
        }
    }
}
