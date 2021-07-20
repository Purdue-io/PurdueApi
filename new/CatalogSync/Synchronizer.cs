using Microsoft.EntityFrameworkCore;
using PurdueIo.Database;
using PurdueIo.Database.Models;
using PurdueIo.Scraper;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DatabaseTerm = PurdueIo.Database.Models.Term;
using DatabaseSubject = PurdueIo.Database.Models.Subject;
using ScrapedSubject = PurdueIo.Scraper.Models.Subject;
using DatabaseSection = PurdueIo.Database.Models.Section;
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

        private CacheSet cacheSet;

        private Synchronizer(IScraper scraper, ApplicationDbContext dbContext)
        {
            this.scraper = scraper;
            this.dbContext = dbContext;

            // Disable EF query tracking to improve performance
            // (we'll do it all ourselves)
            this.dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            // CacheSet will store lookups so we don't hit the DB too many times
            this.cacheSet = new CacheSet();
        }
        
        private async Task SynchronizeTermSubjectAsync(DatabaseTerm dbTerm,
            ScrapedSubject scrapedSubject)
        {
            // Fetch or create the subject in the DB
            var dbSubjectId = HydrateSubject(scrapedSubject.Code, scrapedSubject.Name);

            // Cache courses for this subject
            Dictionary<(string number, string title), Course> dbCourseCache = dbContext.Courses
                .Where(c => c.SubjectId == dbSubjectId)
                .ToDictionary(c => (c.Number, c.Title));
            dbCourseCache.ToList().ForEach(cc => cacheSet.Courses.Add(cc.Key, cc.Value));

            // Cache sections
            Dictionary<string, DatabaseSection> dbSectionCache = dbContext.Sections
                .Include(s => s.Class)
                .Include(s => s.Meetings)
                .ThenInclude(m => m.Room)
                .ThenInclude(r => r.Building)
                .Where(s => s.Class.TermId == dbTerm.Id && s.Class.Course.SubjectId == dbSubjectId)
                .ToDictionary(s => s.Crn);
            dbSectionCache.ToList().ForEach(cs => cacheSet.Sections.Add(cs.Key, cs.Value));

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

                // Check to see if we have this section cached from the DB. 
                // We can use it to hydrate existing relationships.
                Guid? dbCampusId = null;
                Guid? dbCourseId = null;
                Guid? dbClassId = null;
                var firstCachedSection = sectionGroup
                    .Where(s => cacheSet.Sections.ContainsKey(s.Crn))
                    .Select(s => cacheSet.Sections[s.Crn]).FirstOrDefault();
                if (firstCachedSection != null)
                {
                    dbCampusId = firstCachedSection.Class.CampusId;
                    dbCourseId = firstCachedSection.Class.CourseId;
                    dbClassId = firstCachedSection.ClassId;
                }

                // Find or create campus
                if (dbCampusId == null)
                {
                    var sectionWithCampus = 
                        sectionGroup.FirstOrDefault(s => s.CampusCode.Length > 0);
                    if (sectionWithCampus == null)
                    {
                        dbCampusId = HydrateCampus("", "");
                    }
                    else
                    {
                        dbCampusId = HydrateCampus(sectionWithCampus.CampusCode,
                            sectionWithCampus.CampusName);
                    }
                }

                // Find or create course
                if (dbCourseId == null)
                {
                    dbCourseId = HydrateCourse(dbSubjectId, sectionWithCourseInfo, sectionGroup);
                }

                // Create class
                // (if dbClassId is null, none of these courses have an existing course)
                if (dbClassId == null)
                {
                    dbClassId = AddClass((Guid)dbCampusId, (Guid)dbCourseId, dbTerm.Id);
                }
            }
        }

        private Guid HydrateSubject(string subjectAbbreviation, string subjectName)
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
                dbContext.Subjects.AsTracking().Add(dbSubject);
                dbContext.SaveChanges();
            }
            return dbSubject.Id;
        }

        private Guid HydrateCampus(string campusCode, string campusName)
        {
            // Check cache
            if (cacheSet.Campuses.ContainsKey((campusCode, campusName)))
            {
                return cacheSet.Campuses[(campusCode, campusName)].Id;
            }

            // Not cached, is there one in the DB?
            var dbCampus = dbContext.Campuses
                .SingleOrDefault(c => EF.Functions.Like(c.Code, campusCode) && 
                    EF.Functions.Like(c.Name, campusName));
            if (dbCampus != null)
            {
                cacheSet.Campuses[(dbCampus.Code, dbCampus.Name)] = dbCampus;
                return dbCampus.Id;
            }

            // Not in DB, create a new one
            dbCampus = new Campus()
            {
                Id = Guid.NewGuid(),
                Code = campusCode,
                Name = campusName
            };
            dbContext.Campuses.Add(dbCampus);
            dbContext.SaveChanges();
            cacheSet.Campuses[(dbCampus.Code, dbCampus.Name)] = dbCampus;
            return dbCampus.Id;
        }

        private Guid HydrateCourse(Guid dbSubjectId, ScrapedSection sectionWithCourseInfo,
            ICollection<ScrapedSection> sectionGroup)
        {
            // Check cache
            var courseKey = (sectionWithCourseInfo.CourseNumber, sectionWithCourseInfo.CourseTitle);
            if (cacheSet.Courses.ContainsKey(courseKey))
            {
                return cacheSet.Courses[courseKey].Id;
            }

            // Not cached, is there one in the DB?
            var dbCourse = dbContext.Courses.SingleOrDefault(c =>
                (c.SubjectId == dbSubjectId) &&
                EF.Functions.Like(c.Number, sectionWithCourseInfo.CourseNumber) && 
                EF.Functions.Like(c.Title, sectionWithCourseInfo.CourseTitle));
            if (dbCourse != null)
            {
                cacheSet.Courses[courseKey] = dbCourse;
                return dbCourse.Id;
            }

            // Not in DB, create a new one
            dbCourse = new Course()
            {
                Id = Guid.NewGuid(),
                Number = sectionWithCourseInfo.CourseNumber,
                SubjectId = dbSubjectId,
                Title = sectionWithCourseInfo.CourseTitle,
                CreditHours = sectionGroup
                    .OrderByDescending(c => c.CreditHours)
                    .FirstOrDefault()
                    .CreditHours,
                Description = sectionWithCourseInfo.Description,
            };
            dbContext.Courses.Add(dbCourse);
            dbContext.SaveChanges();
            cacheSet.Courses[courseKey] = dbCourse;
            return dbCourse.Id;
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
    }
}