using CatalogSync.Models;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Microsoft.AspNet.Identity.EntityFramework;
using PurdueIoDb;
using PurdueIoDb.Catalog;

namespace CatalogSync
{
	// To learn more about Microsoft Azure WebJobs, please see http://go.microsoft.com/fwlink/?LinkID=401557
	class Program
	{
		private CatalogApi Api;
		static int Main()
		{
			var appSettings = ConfigurationManager.AppSettings;
			var p = new Program(appSettings["MyPurdueUser"], appSettings["MyPurduePass"]); // Credentials go here.
			Database.SetInitializer<ApplicationDbContext>(new MigrateDatabaseToLatestVersion<ApplicationDbContext, PurdueIoDb.Migrations.Configuration>()); 
			p.Synchronize().GetAwaiter().GetResult();
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

		public async Task Synchronize()
		{
			Console.WriteLine(DateTimeOffset.Now.ToString("G") + " Beginning synchronization...");
			var terms = await Api.FetchTermList();
			// Let's synchronize the first term that isn't STAR
			var selectedTerm = terms.Where(t => !t.Name.ToUpper().StartsWith("STAR")).FirstOrDefault();
			Console.WriteLine(DateTimeOffset.Now.ToString("G") + " Synchronizing term '" + selectedTerm.Name + "'...");
			var subjects = await Api.FetchSubjectList(selectedTerm.Id);
			Console.WriteLine("Found " + subjects.Count + " subjects");
			foreach (var subject in subjects)
			{
				Console.Write(DateTimeOffset.Now.ToString("G") + " Synchronizing " + subject.SubjectCode + " / " + subject.SubjectName + ": ");
				await SyncSubject(selectedTerm, subject);
				Console.WriteLine("complete.");
			}
			Console.WriteLine(DateTimeOffset.Now.ToString("G") + " Synchronization of term '" + selectedTerm.Name + "' complete.");
		}

		public async Task SyncSubject(MyPurdueTerm term, MyPurdueSubject subject)
		{
			// Pull all sections for the specified term and subject.
			Dictionary<string, MyPurdueSection> sectionsByCrn;
			try
			{
				sectionsByCrn = await Api.FetchSections(term.Id, subject.SubjectCode);
			}
			catch (Exception)
			{
				Console.WriteLine("\nERROR FETCHING SUBJECT SECTIONS\n");
				return;
			}
			
			Console.Write("+");

			// We have all the section data - now we need to build classes out of them.
			var sectionGroups = new List<List<MyPurdueSection>>();
			var sectionFlatList = new List<MyPurdueSection>(sectionsByCrn.Values);
			int totalSectionCount = sectionFlatList.Count;
			while (sectionFlatList.Count > 0)
			{
				var sectionGroup = new List<MyPurdueSection>();
				_ProcessLinks(ref sectionFlatList, ref sectionGroup, sectionFlatList.Last());
				sectionGroups.Add(sectionGroup);
			}
			Console.Write("+");

			// Now let's sync up the database...
			int sectionCount = 0; // Keep a count of every section we've processed.
			using (var db = new ApplicationDbContext())
			{
				// Check that the term exists
				Term dbTerm = db.Terms.Where(t => t.TermCode.ToUpper() == term.Id.ToUpper()).FirstOrDefault();
				if (dbTerm == null)
				{
					dbTerm = new Term()
					{
						TermId = Guid.NewGuid(),
						TermCode = term.Id,
						Name = term.Name,
						Classes = new List<Class>(),
						StartDate = DateTimeOffset.MinValue,
						EndDate = DateTimeOffset.MinValue
					};
					db.Terms.Add(dbTerm);
				}

				foreach (var group in sectionGroups)
				{
					var subj = group.First().SubjectCode;
					var number = group.First().Number;
					var title = group.First().Title;
					var description = group.First().Description;
					var creditHours = group.OrderByDescending(s => s.CreditHours).Select(s => s.CreditHours).FirstOrDefault();

					// Check that the campus exists
					var campusName = group.First().CampusName;
					var campusCode = group.First().CampusCode;
					Campus dbCampus = db.Campuses.Where(c => c.Code.ToUpper() == campusCode.ToUpper()).FirstOrDefault();
					if (dbCampus == null)
					{
						dbCampus = new Campus()
						{
							CampusId = Guid.NewGuid(),
							Code = group.First().CampusCode,
							Name = group.First().CampusName,
							Buildings = new List<Building>(),
							ZipCode = ""
						};
						db.Campuses.Add(dbCampus);
						db.SaveChanges();
					}

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
							Name = subj.ToUpper().Equals(subject.SubjectCode.ToUpper()) ? subject.SubjectName : "",
							Courses = new List<Course>(),
							Abbreviation = subj.ToUpper()
						};
						db.Subjects.Add(dbSubj);
						db.SaveChanges();
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
						db.SaveChanges();
					}

					// Check that the class exists
					// We do this by looking up each CRN until we find one that exists and has a class in the same term
					Class dbClass;
					var validCrns = group.Select(g => g.Crn);
					var dbSections = db.Sections.Where(s => validCrns.Contains(s.CRN) && s.Class.Term.TermId == dbTerm.TermId);
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
						db.SaveChanges();
					}

					// Add all of the sections ...
					foreach (var section in group)
					{
						Section dbSection = db.Sections.Where(s => s.CRN == section.Crn && s.Class.Term.TermId == dbTerm.TermId).FirstOrDefault();
						if (dbSection == null)
						{
							dbSection = new Section()
							{
								SectionId = Guid.NewGuid(),
								CRN = section.Crn,
								Meetings = new List<Meeting>()
							};
							db.Sections.Add(dbSection);
						}
						// Update all the information (whether it exists or not)
						dbSection.Class = dbClass;
						dbSection.Type = section.Type;
						dbSection.StartDate = section.Meetings.OrderBy(m => m.StartDate).Select(m => m.StartDate).First();
						dbSection.EndDate = section.Meetings.OrderByDescending(m => m.EndDate).Select(m => m.EndDate).First();
						dbSection.Capacity = section.Capacity;
						dbSection.Enrolled = section.Enrolled;
						dbSection.RemainingSpace = section.RemainingSpace;
						dbSection.WaitlistCapacity = section.WaitlistCapacity;
						dbSection.WaitlistCount = section.WaitlistCount;
						dbSection.WaitlistSpace = section.WaitlistSpace;
						dbSection.RegistrationStatus = RegistrationStatus.NotAvailable;

						// First, delete any meetings that don't exist in the latest pull
						foreach (var meeting in dbSection.Meetings)
						{
							// TODO: compare instructors
							var matches = section.Meetings.Where(m =>
									m.Type == meeting.Type &&
									m.StartTime == meeting.StartTime &&
									m.EndTime == meeting.StartTime.Add(meeting.Duration) &&
									m.DaysOfWeek == meeting.DaysOfWeek &&
									m.BuildingCode == meeting.Room.Building.ShortCode &&
									m.RoomNumber == meeting.Room.Number
								);
							if (matches.Count() <= 0)
							{
								dbSection.Meetings.Remove(meeting);
								db.Meetings.Remove(meeting);
							}
						}

						// Add all of the meetings that don't exist.
						foreach (var meeting in section.Meetings)
						{
							var matches = dbSection.Meetings.Where(m =>
									m.Type == meeting.Type &&
									m.StartTime == meeting.StartTime &&
									m.Duration == meeting.EndTime.Subtract(meeting.StartTime) &&
									m.DaysOfWeek == meeting.DaysOfWeek &&
									m.Room.Building.ShortCode == meeting.BuildingCode &&
									m.Room.Number == meeting.RoomNumber
								);
							if (matches.Count() <= 0)
							{
								// make sure instructors exist
								var newInstructors = new List<Instructor>();
								foreach (var inst in meeting.Instructors)
								{
									var instructorMatch = db.Instructors.Where(i => i.Email == inst.Item2).FirstOrDefault();
									if (instructorMatch == null)
									{
										instructorMatch = new Instructor()
										{
											InstructorId = Guid.NewGuid(),
											Email = inst.Item2,
											Name = inst.Item1,
											Meetings = new List<Meeting>()
										};
										db.Instructors.Add(instructorMatch);
										db.SaveChanges(); // Have to save changes after an add op
									}
									newInstructors.Add(instructorMatch);
								}

								// make sure the building exists
								var buildingMatch = db.Buildings.Where(b => b.ShortCode.ToUpper() == meeting.BuildingCode.ToUpper() && b.Campus.CampusId == dbCampus.CampusId).FirstOrDefault();
								if (buildingMatch == null)
								{
									buildingMatch = new Building()
									{
										BuildingId = Guid.NewGuid(),
										Campus = dbCampus,
										Name = meeting.BuildingName,
										ShortCode = meeting.BuildingCode,
										Rooms = new List<Room>()
									};
									db.Buildings.Add(buildingMatch);
									db.SaveChanges();
								}

								// make sure the room exists
								var roomMatch = db.Rooms.Where(r => r.Building.BuildingId == buildingMatch.BuildingId && r.Number == meeting.RoomNumber).FirstOrDefault();
								if (roomMatch == null)
								{
									roomMatch = new Room()
									{
										RoomId = Guid.NewGuid(),
										Building = buildingMatch,
										Number = meeting.RoomNumber,
										Meetings = new List<Meeting>()
									};
									db.Rooms.Add(roomMatch);
									db.SaveChanges();
								}

								// Create the actual meeting object
								var newMeeting = new Meeting()
								{
									MeetingId = Guid.NewGuid(),
									Type = meeting.Type,
									StartTime = meeting.StartTime,
									Duration = meeting.EndTime.Subtract(meeting.StartTime),
									Instructors = newInstructors,
									Room = roomMatch,
									DaysOfWeek = meeting.DaysOfWeek,
									Section = dbSection,
									StartDate = meeting.StartDate,
									EndDate = meeting.EndDate
								};
								dbSection.Meetings.Add(newMeeting);
							}
						}
						sectionCount++;
						if (sectionCount % (int)(totalSectionCount/10.0) == 0)
						{
							Console.Write(".");
						}
					}
					db.SaveChanges();
				}
			}
		}

		private void _ProcessLinks(ref List<MyPurdueSection> sourceList, ref List<MyPurdueSection> outList, MyPurdueSection sourceSection)
		{
			sourceList.Remove(sourceSection);
			outList.Add(sourceSection);
			var candidates = sourceList.Where(s => s.LinkSelf.Length > 0 && s.LinkSelf == sourceSection.LinkOther && s.Number == sourceSection.Number && s.SubjectCode == sourceSection.SubjectCode).Distinct().ToList();
			if (candidates.Count() <= 0) return;
			//outList.AddRange(candidates);
			foreach (var candidate in candidates)
			{
				sourceList.Remove(candidate);
			}
			foreach (var candidate in candidates)
			{
				_ProcessLinks(ref sourceList, ref outList, candidate);
			}
		}
	}
}
