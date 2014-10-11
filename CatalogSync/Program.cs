using CatalogSync.Models;
using PurdueIo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using PurdueIo.Models.Catalog;

namespace CatalogSync
{
	// To learn more about Microsoft Azure WebJobs, please see http://go.microsoft.com/fwlink/?LinkID=401557
	class Program
	{
		private CatalogApi Api;
		public readonly string CAMPUSNAME = "Purdue University West Lafayette";
		public readonly string CAMPUSZIP = "47906";
		static int Main()
		{
			var p = new Program("", ""); // Credentials go here.
			p.SyncCourses("201510").GetAwaiter().GetResult();
			Console.ReadLine();
			return 0;
		}

		Program(string user, string pass) {
			Api = new CatalogApi(user, pass);
			var catalogApi = new CatalogApi(user, pass);
			var login = catalogApi.HasValidCredentials().Result;
			if (!login) {
				throw new UnauthorizedAccessException("Could not authenticate to myPurdue with the supplied credentials.");
			}
		}

		public async Task SyncCourses(string termId)
		{
			var sectionsByCrn = await Api.FetchSectionList(termId, "CS"); // test CS for now.

			foreach (var section in sectionsByCrn.Values)
			{
				Console.WriteLine();
				Console.WriteLine(section.Crn + " | " + section.SubjectCode + section.Number + " / " + section.Title + " (" + section.Type + ") " + section.LinkSelf + " | " + section.LinkOther);
				Console.WriteLine("Cap: " + section.Capacity + ", Act: " + section.Enrolled + ", Rem: " + section.RemainingSpace);
				foreach (var meeting in section.Meetings)
				{
					var inst = (meeting.Instructors != null && meeting.Instructors.Count > 0) ? meeting.Instructors.Select(x => x.Item1).Aggregate((x, y) => x + ", " + y) : "" ;
					Console.WriteLine("\t " + inst + " in " + meeting.BuildingName + " (" + meeting.BuildingCode + ") " + meeting.RoomNumber + " @ " + meeting.StartTime.ToString("t") + " - " + meeting.EndTime.ToString("t"));
				}
			}

			// We have all the section data - now we need to build classes out of them.
			
			var sectionGroups = new List<List<MyPurdueSection>>();
			var sectionFlatList = new List<MyPurdueSection>(sectionsByCrn.Values);

			while (sectionFlatList.Count > 0)
			{
				var sectionGroup = new List<MyPurdueSection>();
				ProcessLinks(ref sectionFlatList, ref sectionGroup, sectionFlatList.Last());
				sectionGroups.Add(sectionGroup);
			}

			// Now let's sync up the database...
			using (var db = new ApplicationDbContext())
			{
				// Check that the term exists
				Term dbTerm = db.Terms.Where(t => t.TermCode.ToUpper() == termId.ToUpper()).FirstOrDefault();
				if (dbTerm == null)
				{
					dbTerm = new Term()
					{
						TermId = Guid.NewGuid(),
						TermCode = termId,
						Classes = new List<Class>(),
						StartDate = DateTimeOffset.MinValue,
						EndDate = DateTimeOffset.MinValue
					};
					db.Terms.Add(dbTerm);
				}

				// Check that the campus exists
				Campus dbCampus = db.Campuses.Where(c => c.Name.ToUpper() == CAMPUSNAME.ToUpper()).FirstOrDefault();
				if (dbCampus == null)
				{
					dbCampus = new Campus()
					{
						CampusId = Guid.NewGuid(),
						Name = CAMPUSNAME,
						Buildings = new List<Building>(),
						ZipCode = CAMPUSZIP
					};
					db.Campuses.Add(dbCampus);
				}

				foreach (var group in sectionGroups)
				{
					var subj = group.First().SubjectCode;
					var number = group.First().Number;
					var title = group.First().Title;
					var description = group.First().Description;
					var creditHours = group.OrderByDescending(s => s.CreditHours).Select(s => s.CreditHours).FirstOrDefault();

					// Check that the subject exists
					Subject dbSubj;
					var subjCheck = db.Subjects.Where(s => s.Abbreviation.ToUpper() == subj.ToUpper());
					if (subjCheck.Count() > 0)
					{
						dbSubj = subjCheck.First();
					}
					else
					{
						dbSubj = new Subject()
						{
							SubjectId = Guid.NewGuid(),
							Name = "",
							Courses = new List<Course>(),
							Abbreviation = subj.ToUpper()
						};
						db.Subjects.Add(dbSubj);
					}

					// Check that the course exists
					Course dbCourse;
					var courseCheck = db.Courses.Where(c => c.Subject.SubjectId == dbSubj.SubjectId && c.Number == number && c.Title == title);
					if (courseCheck.Count() > 0)
					{
						dbCourse = courseCheck.First();
					}
					else
					{
						dbCourse = new Course()
						{
							CourseId = Guid.NewGuid(),
							Subject = dbSubj,
							Number = number,
							Title = title,
							Description = description,
							CreditHours = creditHours,
							Classes = new List<Class>()
						};
						db.Courses.Add(dbCourse);
					}

					// Check that the class exists
					// We do this by looking up each CRN until we find one that exists and has a class
					Class dbClass;
					var validCrns = group.Select(g => g.Crn);
					var dbSections = db.Sections.Where(s => validCrns.Contains(s.CRN));
					if (dbSections.Count() > 0)
					{
						dbClass = dbSections.First().Class;
					}
					else
					{
						dbClass = new Class()
						{
							ClassId = Guid.NewGuid(),
							Campus = dbCampus,
							Term = dbTerm,
							Course = dbCourse,
							Sections = new List<Section>()
						};
						db.Classes.Add(dbClass);
					}

					// Add all of the sections ...
					foreach (var section in group)
					{
						Section dbSection = db.Sections.Where(s => s.CRN == section.Crn).FirstOrDefault();
						if (dbSection == null)
						{
							dbSection = new Section()
							{
								SectionId = Guid.NewGuid(),
								Class = dbClass,
								CRN = section.Crn,
								Type = section.Type,
								StartDate = section.Meetings.OrderBy(m => m.StartDate).Select(m => m.StartDate).First(),
								EndDate = section.Meetings.OrderByDescending(m => m.EndDate).Select(m => m.EndDate).First(),
								Capacity = section.Capacity,
								Enrolled = section.Enrolled,
								RemainingSpace = section.RemainingSpace,
								WaitlistCapacity = section.WaitlistCapacity,
								WaitlistCount = section.WaitlistCount,
								WaitlistSpace = section.WaitlistSpace,
								RegistrationStatus = RegistrationStatus.NotAvailable,
								Meetings = new List<Meeting>()
							};
							db.Sections.Add(dbSection);
						}

						// Add all of the meetings ...
						foreach (var meeting in section.Meetings)
						{

						}
					}
					db.SaveChanges();
				}
			}
			Console.WriteLine("Sync complete for " + termId);
		}

		private void ProcessLinks(ref List<MyPurdueSection> sourceList, ref List<MyPurdueSection> outList, MyPurdueSection sourceSection)
		{
			sourceList.Remove(sourceSection);
			outList.Add(sourceSection);
			var candidates = sourceList.Where(s => s.LinkSelf.Length > 0 && (s.LinkSelf == sourceSection.LinkSelf || s.LinkSelf == sourceSection.LinkOther) && s.Number == sourceSection.Number && s.SubjectCode == sourceSection.SubjectCode).Distinct().ToList();
			if (candidates.Count() <= 0) return;
			outList.AddRange(candidates);
			foreach (var candidate in candidates)
			{
				sourceList.Remove(candidate);
			}
			foreach (var candidate in candidates)
			{
				ProcessLinks(ref sourceList, ref outList, candidate);
			}
		}
	}
}
