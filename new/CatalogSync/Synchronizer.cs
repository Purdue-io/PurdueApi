using Microsoft.EntityFrameworkCore;
using PurdueIo.Database;
using PurdueIo.Database.Models;
using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DatabaseCampus = PurdueIo.Database.Models.Campus;
using DatabaseClass = PurdueIo.Database.Models.Class;
using DatabaseCourse = PurdueIo.Database.Models.Course;
using DatabaseMeeting = PurdueIo.Database.Models.Meeting;
using DatabaseSection = PurdueIo.Database.Models.Section;
using DatabaseSubject = PurdueIo.Database.Models.Subject;
using DatabaseTerm = PurdueIo.Database.Models.Term;
using ScrapedSubject = PurdueIo.Scraper.Models.Subject;
using ScrapedSection = PurdueIo.Scraper.Models.Section;

namespace PurdueIo.CatalogSync
{
    public class Synchronizer
    {
        public static Task SynchronizeAsync(IScraper scraper, ApplicationDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        public static Task SynchronizeTermAsync(string termCode, IScraper scraper,
            ApplicationDbContext dbContext)
        {
            throw new NotImplementedException();
        }

        private IScraper scraper;

        private ApplicationDbContext dbContext;

        private Synchronizer(IScraper scraper, ApplicationDbContext dbContext)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;
        }
        
        private async Task SynchronizeTermSubjectAsync(DatabaseTerm dbTerm,
            ScrapedSubject scrapedSubject)
        {
            // Fetch or create the subject in the DB
            var dbSubject = HydrateSubject(scrapedSubject.Code, scrapedSubject.Name);

            // Scrape sections
            ICollection<ScrapedSection> scrapedSections =
                await scraper.GetSectionsAsync(dbTerm.Code, scrapedSubject.Code);

            // Group sections
            ICollection<ICollection<ScrapedSection>> groupedSections = 
                SectionLinker.GroupLinkedSections(scrapedSections);

            foreach (var sectionGroup in groupedSections)
            {
                var sectionWithCourseInfo = sectionGroup
                    .FirstOrDefault(s => (s.CourseNumber.Length > 0) && (s.CourseTitle.Length > 0));
                if (sectionWithCourseInfo == null)
                {
                    Console.WriteLine("WARNING: No course information found for CRNs " + 
                        $"{string.Join(", ", sectionGroup.Select(s => s.Crn))}");
                    continue;
                }

                Console.WriteLine($"{sectionWithCourseInfo.SubjectCode}" +
                    $"{sectionWithCourseInfo.CourseNumber} - " +
                    $"{sectionWithCourseInfo.CourseTitle}...");

                // Find or create campus
                var sectionWithCampus = 
                    sectionGroup.FirstOrDefault(s => s.CampusCode.Length > 0);
                DatabaseCampus dbCampus;
                if (sectionWithCampus == null)
                {
                    dbCampus = HydrateCampus("", "");
                }
                else
                {
                    dbCampus = HydrateCampus(sectionWithCampus.CampusCode,
                        sectionWithCampus.CampusName);
                }

                // Find or create course
                var dbCourse = HydrateCourse(dbSubject, sectionWithCourseInfo, sectionGroup);

                // Create class
                var dbClass = HydrateClass(dbCampus, dbCourse, dbTerm, sectionGroup);

                foreach (var section in sectionGroup)
                {
                    UpdateSection(section, dbClass);
                }
            }
        }

        private DatabaseSubject HydrateSubject(string subjectAbbreviation, string subjectName)
        {
            var dbSubject = dbContext.Subjects
                .SingleOrDefault(s => EF.Functions.Like(s.Abbreviation, subjectAbbreviation));
            if (dbSubject == null)
            {
                dbSubject = new DatabaseSubject()
                {
                    Id = Guid.NewGuid(),
                    Abbreviation = subjectAbbreviation,
                    Name = subjectName,
                };
                dbContext.Subjects.Add(dbSubject);
            }
            return dbSubject;
        }

        private DatabaseCampus HydrateCampus(string campusCode, string campusName)
        {
            var dbCampus = dbContext.Campuses
                .SingleOrDefault(c => EF.Functions.Like(c.Code, campusCode) && 
                    EF.Functions.Like(c.Name, campusName));
            if (dbCampus != null)
            {
                return dbCampus;
            }

            // Not in DB, create a new one
            dbCampus = new Campus()
            {
                Id = Guid.NewGuid(),
                Code = campusCode,
                Name = campusName
            };
            dbContext.Campuses.Add(dbCampus);
            return dbCampus;
        }

        private DatabaseCourse HydrateCourse(DatabaseSubject dbSubject, ScrapedSection sectionWithCourseInfo,
            ICollection<ScrapedSection> sectionGroup)
        {
            var dbCourse = dbContext.Courses.SingleOrDefault(c =>
                (c.SubjectId == dbSubject.Id) &&
                EF.Functions.Like(c.Number, sectionWithCourseInfo.CourseNumber) && 
                EF.Functions.Like(c.Title, sectionWithCourseInfo.CourseTitle));
            if (dbCourse != null)
            {
                return dbCourse;
            }

            // Not in DB, create a new one
            dbCourse = new Course()
            {
                Id = Guid.NewGuid(),
                Number = sectionWithCourseInfo.CourseNumber,
                SubjectId = dbSubject.Id,
                Title = sectionWithCourseInfo.CourseTitle,
                CreditHours = sectionGroup
                    .OrderByDescending(c => c.CreditHours)
                    .FirstOrDefault()
                    .CreditHours,
                Description = sectionWithCourseInfo.Description,
            };
            dbContext.Courses.Add(dbCourse);
            return dbCourse;
        }

        private DatabaseClass HydrateClass(DatabaseCampus dbCampus, DatabaseCourse dbCourse,
            DatabaseTerm dbTerm, ICollection<ScrapedSection> sectionGroup)
        {
            var dbClass = dbContext.Sections
                .Where(dbS => sectionGroup.Any(s => s.Crn == dbS.Crn))
                .Select(s => s.Class)
                .FirstOrDefault();

            if (dbClass == null)
            {
                dbClass = new DatabaseClass()
                {
                    Id = Guid.NewGuid(),
                    CourseId = dbCourse.Id,
                    TermId = dbTerm.Id,
                    CampusId = dbCampus.Id,
                    Sections = new List<DatabaseSection>(),
                };
                dbContext.Classes.Add(dbClass);
            }

            return dbClass;
        }

        private Guid AddClass(Guid dbCampusId, Guid dbCourseId, Guid dbTermId)
        {
            var dbClass = new Class()
            {
                Id = Guid.NewGuid(),
                CourseId = dbCourseId,
                TermId = dbTermId,
                CampusId = dbCampusId,
            };
            dbContext.Classes.Add(dbClass);
            dbContext.SaveChanges();
            // We don't cache classes, since we pull down that information with the section cache
            return dbClass.Id;
        }

        private void UpdateSection(ScrapedSection section, DatabaseClass dbClass)
        {
            // Find DatabaseSection
            DatabaseSection dbSection = dbContext.Sections
                .Include(s => s.Meetings)
                .SingleOrDefault(c => c.Crn == section.Crn);

            var startDate = section.Meetings
                .OrderBy(m => m.StartDate)
                .Select(m => m.StartDate)
                .First();
            var endDate = section.Meetings
                .OrderByDescending(m => m.EndDate)
                .Select(m => m.EndDate)
                .First();

            // If still null, we've never seen this section before.
            // Create a new one.
            if (dbSection == null)
            {
                dbSection = new DatabaseSection()
                {
                    Id = Guid.NewGuid(),
                    Crn = section.Crn,
                    ClassId = dbClass.Id,
                    Meetings = new List<DatabaseMeeting>(),
                    Type = section.Type,
                    RegistrationStatus = RegistrationStatus.NotAvailable,
                    StartDate = startDate,
                    EndDate = endDate,
                    Capacity = section.Capacity,
                    Enrolled = section.Enrolled,
                    RemainingSpace = section.RemainingSpace,
                    WaitListCapacity = section.WaitListCapacity,
                    WaitListCount = section.WaitListCount,
                    WaitListSpace = section.WaitListSpace,
                };
                dbContext.Sections.Add(dbSection);
            }
            else
            {
                // Update fields to match the new scraped data
                if (dbSection.ClassId != dbClass.Id)
                {
                    dbSection.ClassId = dbClass.Id;
                }
                if (dbSection.Type != section.Type)
                {
                    dbSection.Type = section.Type;
                }
                // TODO: Registration status..?
                if (dbSection.StartDate != startDate)
                {
                    dbSection.StartDate = startDate;
                }
                if (dbSection.EndDate != endDate)
                {
                    dbSection.EndDate = endDate;
                }
                if (dbSection.Capacity != section.Capacity)
                {
                    dbSection.Capacity = section.Capacity;
                }
                if (dbSection.Enrolled != section.Enrolled)
                {
                    dbSection.Enrolled = section.Enrolled;
                }
                if (dbSection.RemainingSpace != section.RemainingSpace)
                {
                    dbSection.RemainingSpace = section.RemainingSpace;
                }
                if (dbSection.WaitListCapacity != section.WaitListCapacity)
                {
                    dbSection.WaitListCapacity = section.WaitListCapacity;
                }
                if (dbSection.WaitListCount != section.WaitListCount)
                {
                    dbSection.WaitListCount = section.WaitListCount;
                }
                if (dbSection.WaitListSpace != section.WaitListSpace)
                {
                    dbSection.WaitListSpace = section.WaitListSpace;
                }
            }

            // Update meetings for this section
            UpdateSectionMeetings(dbSection, section);
        }

        private void UpdateSectionMeetings(DatabaseSection dbSection, ScrapedSection section)
        {

        }
    }
}