using CatalogApi;
using CatalogApi.Models;
using PurdueIoDb;
using PurdueIoDb.Catalog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogSync
{
    /// <summary>
    /// This class is responsible for syncing a single term.
    /// </summary>
    public class TermSync : IDisposable
    {
        /// <summary>
        /// The TermId as referenced in the database
        /// </summary>
        private Guid TermId;

        /// <summary>
        /// The term code as referenced in the Purdue catalog (e.g. 201510)
        /// </summary>
        private string TermCode;

        /// <summary>
        /// The term name (e.g. Fall 2015)
        /// </summary>
        private string TermName;

        /// <summary>
        /// Reference to CatalogApi to query MyPurdue
        /// </summary>
        private CatalogApi.CatalogApi Api;

        #region Database Caches
        /// <summary>
        /// Maintains a cache of Campus GUIDs by campus code
        /// </summary>
        private Dictionary<string, Guid> CampusCache = new Dictionary<string, Guid>();

        /// <summary>
        /// Maintains a cache of Subject GUIDs by subject abbreviation
        /// </summary>
        private Dictionary<string, Guid> SubjectCache = new Dictionary<string, Guid>();

        /// <summary>
        /// Maintains a cache of Instructor GUIDs by email
        /// </summary>
        private Dictionary<string, Guid> InstructorCache = new Dictionary<string, Guid>();

        /// <summary>
        /// Maintains a cache of Building GUIDs by Campus GUID and Building code
        /// </summary>
        private Dictionary<Tuple<Guid, string>, Guid> BuildingCache = new Dictionary<Tuple<Guid, string>, Guid>();

        /// <summary>
        /// Maintains a cache of Room GUIDs by Building GUID and Room number
        /// </summary>
        private Dictionary<Tuple<Guid, string>, Guid> RoomCache = new Dictionary<Tuple<Guid, string>, Guid>();
        #endregion

        /// <summary>
        /// Instantiate a new TermSync with the specified term
        /// </summary>
        /// <param name="code">Term code as specified by Purdue catalog (e.g. 201510)</param>
        /// <param name="name">Term name (e.g. Fall 2015)</param>
        public TermSync(string code, string name, CatalogApi.CatalogApi api)
        { 
            TermCode = code;
            TermName = name;
            Api = api;
        }

        public async Task Synchronize()
        {
            Console.WriteLine("Syncronizing Term " + TermCode + " / " + TermName + " ...");
            using (var db = new ApplicationDbContext())
            {
                Term dbTerm = db.Terms.SingleOrDefault(x => x.TermCode == TermCode);
                if (dbTerm == null)
                {
                    dbTerm = new Term()
                    {
                        TermId = Guid.NewGuid(),
                        Name = TermName,
                        TermCode = TermCode,
                        Classes = new List<Class>(),
                        StartDate = DateTimeOffset.MinValue,
                        EndDate = DateTimeOffset.MinValue
                    };
                    db.Terms.Add(dbTerm);
                    await db.SaveChangesAsync();
                    Console.WriteLine("\tTerm was added to the database.");
                }
                TermId = dbTerm.TermId;
            }

            // Cache all campuses
            using (var db = new ApplicationDbContext())
            {
                CampusCache = db.Campuses.AsNoTracking().ToDictionary(c => c.Code, c => c.CampusId);
            }

            // Cache all subjects
            using (var db = new ApplicationDbContext())
            {
                SubjectCache = db.Subjects.AsNoTracking().ToDictionary(s => s.Abbreviation, s => s.SubjectId);
            }

            // Cache all buildngs
            using (var db = new ApplicationDbContext())
            {
                BuildingCache = db.Buildings.AsNoTracking().ToDictionary(b => new Tuple<Guid, string>(b.CampusId, b.ShortCode), b => b.BuildingId);
            }

            var subjects = await Api.FetchSubjectList(TermCode);
            Console.WriteLine("\tFound " + subjects.Count + " subjects.");
            foreach (var subject in subjects)
            {
                await SyncSubject(subject);
            }

            // Update term start and end dates
            using (var db = new ApplicationDbContext())
            {
                var dbTerm = db.Terms.SingleOrDefault(t => t.TermId == TermId);
                if (dbTerm != null)
                {
                    var earliestSection = db.Sections.Where(s => s.Class.Term.TermId == dbTerm.TermId).Where(s => s.StartDate != DateTimeOffset.MinValue).OrderBy(s => s.StartDate).FirstOrDefault();
                    var latestSection = db.Sections.Where(s => s.Class.Term.TermId == dbTerm.TermId).Where(s => s.EndDate != DateTimeOffset.MinValue).OrderByDescending(s => s.EndDate).FirstOrDefault();
                    if (earliestSection != null && latestSection != null)
                    {
                        dbTerm.StartDate = earliestSection.StartDate;
                        dbTerm.EndDate = latestSection.EndDate;
                        db.SaveChanges();
                        Console.WriteLine("\tUpdating term dates: " + dbTerm.StartDate.ToString("d") + " - " + dbTerm.EndDate.ToString("d"));
                    }
                }
            }
        }

        private async Task SyncSubject(MyPurdueSubject subject)
        {
            Console.WriteLine("\tSynchronizing " + subject.SubjectCode + " / " + subject.SubjectName + " ...");
            var dbSubjectId = await EnsureSubject(subject.SubjectCode, subject.SubjectName);

            // Prepare a cache of all courses
            Dictionary<Tuple<string, string>, Course> dbCourseCache;
            using (var db = new ApplicationDbContext())
            {
                dbCourseCache = db.Courses.AsNoTracking().Where(c => c.SubjectId == dbSubjectId)
                    .ToDictionary(c => new Tuple<string, string>(c.Number, c.Title));
            }
            Console.WriteLine("\t\tCached " + dbCourseCache.Count + " courses from database.");

            // Prepare a cache of all sections
            Dictionary<string, Section> dbSectionCache;
            using (var db = new ApplicationDbContext())
            {
                dbSectionCache = db.Sections.AsNoTracking().Include(s => s.Class).Include(s => s.Meetings.Select(m => m.Room.Building))
                    .Where(s => s.Class.TermId == TermId && s.Class.Course.SubjectId == dbSubjectId)
                    .ToDictionary(s => s.CRN);
            }
            Console.WriteLine("\t\tCached " + dbSectionCache.Count + " sections from database.");

            // Fetch sections from MyPurdue
            Dictionary<string, MyPurdueSection> sectionsByCrn = null;
            sectionsByCrn = await Api.FetchSections(TermCode, subject.SubjectCode);
            Console.WriteLine("\t\tFetched " + sectionsByCrn.Count + " sections from MyPurdue.");

            // Group sections into classes by matching links
            var sectionFlatList = new List<MyPurdueSection>(sectionsByCrn.Values);
            var classGroups = GroupSections(sectionFlatList);

            // Process and insert each class
            foreach (var classGroup in classGroups)
            {
                // All of the foreign keys we need to find in order to hook sections up
                Guid? dbCampusId = null;
                Guid? dbCourseId = null;
                Guid? dbClassId = null;

                // Find the section that'll tell us course number and title (all of them should, but to be safe...)
                var sectionWithCourseInfo = classGroup.FirstOrDefault(cg => cg.Number.Length > 0 && cg.Title.Length > 0);
                if (sectionWithCourseInfo == null)
                {
                    Console.WriteLine("\t\t** WARNING: No course information (title / number) found for CRNs " + string.Join(", ", classGroup.Select(c => c.Crn)));
                    continue;
                }
                Console.WriteLine("\t\t" + subject.SubjectCode + sectionWithCourseInfo.Number + " - " + sectionWithCourseInfo.Title + " ...");

                // Do we have any of the sections cached?
                var cachedSection = classGroup.Where(s => dbSectionCache.ContainsKey(s.Crn))
                    .Select(s => dbSectionCache[s.Crn]).FirstOrDefault();

                if (cachedSection != null)
                {
                    dbCampusId = cachedSection.Class.CampusId;
                    dbCourseId = cachedSection.Class.CourseId;
                    dbClassId = cachedSection.ClassId;
                }

                // Find or create any elements we don't have ...

                if (dbCampusId == null)
                {
                    // Find / create the campus
                    var sectionWithCampus = classGroup.FirstOrDefault(cg => cg.CampusCode.Length > 0);
                    if (sectionWithCampus == null)
                    {
                        dbCampusId = await EnsureCampus("", ""); // No campus
                    }
                    else
                    {
                        dbCampusId = await EnsureCampus(sectionWithCampus.CampusCode, sectionWithCampus.CampusName);
                    }
                }

                if (dbCourseId == null)
                {
                    // Find / create the course
                    var courseKey = new Tuple<string, string>(sectionWithCourseInfo.Number, sectionWithCourseInfo.Title);
                    if (dbCourseCache.ContainsKey(courseKey))
                    {
                        dbCourseId = dbCourseCache[courseKey].CourseId;
                    }
                    else
                    {
                        using (var db = new ApplicationDbContext())
                        {
                            var dbCourse = new Course()
                            {
                                CourseId = Guid.NewGuid(),
                                SubjectId = dbSubjectId,
                                Title = sectionWithCourseInfo.Title,
                                Number = sectionWithCourseInfo.Number,
                                Description = sectionWithCourseInfo.Description,
                                CreditHours = classGroup.OrderByDescending(c => c.CreditHours).FirstOrDefault().CreditHours,
                                Classes = new List<Class>()
                            };
                            db.Courses.Add(dbCourse);
                            await db.SaveChangesAsync();
                            dbCourseCache[courseKey] = dbCourse;
                            dbCourseId = dbCourse.CourseId;
                        }
                    }
                }
                
                if (dbClassId == null)
                {
                    // Find / create the class
                    using (var db = new ApplicationDbContext())
                    {
                        var dbClass = new Class()
                        {
                            ClassId = Guid.NewGuid(),
                            CampusId = (Guid)dbCampusId,
                            CourseId = (Guid)dbCourseId,
                            TermId = TermId,
                            Sections = new List<Section>()
                        };
                        db.Classes.Add(dbClass);
                        await db.SaveChangesAsync();
                        dbClassId = dbClass.ClassId;
                    }
                }

                // Update / create each section
                var updatedSections = new List<Section>();
                var newSections = new List<Section>();
                var instructorEntities = new Dictionary<Guid, Instructor>();
                foreach (var section in classGroup)
                {
                    Section dbSection;
                    if (dbSectionCache.ContainsKey(section.Crn))
                    {
                        dbSection = dbSectionCache[section.Crn];
                        var sectionChanged = false;
                        // Check for changes
                        if (dbSection.ClassId != dbClassId
                            || dbSection.Type != section.Type
                            || dbSection.Capacity != section.Capacity
                            || dbSection.Enrolled != section.Enrolled
                            || dbSection.RemainingSpace != section.RemainingSpace
                            || dbSection.WaitlistCapacity != section.WaitlistCapacity
                            || dbSection.WaitlistCount != section.WaitlistCount
                            || dbSection.WaitlistSpace != section.WaitlistSpace)
                        {
                            dbSection.ClassId = (Guid)dbClassId;
                            dbSection.Type = section.Type;
                            dbSection.Capacity = section.Capacity;
                            dbSection.Enrolled = section.Enrolled;
                            dbSection.RemainingSpace = section.RemainingSpace;
                            dbSection.WaitlistCapacity = section.WaitlistCapacity;
                            dbSection.WaitlistCount = section.WaitlistCount;
                            dbSection.WaitlistSpace = section.WaitlistSpace;
                            sectionChanged = true;
                        }

                        // Update meetings
                        // First, delete any meetings that don't exist in the latest MyPurdue pull
                        foreach (var meeting in dbSection.Meetings.ToList())
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
                                sectionChanged = true;
                            }
                        }
                        // Add all of the meetings that don't exist.
                        var existingMeetings = dbSection.Meetings.ToList(); // copy of list to avoid changes during loop
                        foreach (var meeting in section.Meetings)
                        {
                            var matches = existingMeetings.Where(m =>
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
                                var instructors = new List<Instructor>();
                                foreach (var inst in meeting.Instructors)
                                {
                                    var instructorId = await EnsureInstructor(inst.Item2, inst.Item1);
                                    if (!instructorEntities.ContainsKey(instructorId))
                                    {
                                        instructorEntities[instructorId] = new Instructor()
                                        {
                                            InstructorId = instructorId
                                        };
                                    }
                                    instructors.Add(instructorEntities[instructorId]);
                                }

                                // make sure the building exists
                                var dbBuildingId = await EnsureBuilding((Guid)dbCampusId, meeting.BuildingCode, meeting.BuildingName);

                                // make sure the room exists
                                var dbRoomId = await EnsureRoom(dbBuildingId, meeting.RoomNumber);

                                // Create the actual meeting object
                                var newMeeting = new Meeting()
                                {
                                    MeetingId = Guid.NewGuid(),
                                    Type = meeting.Type,
                                    StartTime = meeting.StartTime,
                                    Duration = meeting.EndTime.Subtract(meeting.StartTime),
                                    Instructors = instructors,
                                    RoomId = dbRoomId,
                                    DaysOfWeek = meeting.DaysOfWeek,
                                    Section = dbSection,
                                    StartDate = meeting.StartDate,
                                    EndDate = meeting.EndDate
                                };
                                dbSection.Meetings.Add(newMeeting);
                                sectionChanged = true;
                            }
                        }
                        
                        // If we've made any changes, flag this for committing to the DB later
                        if (sectionChanged)
                        {
                            if (section.Meetings.Count > 0)
                            {
                                var startDate = section.Meetings.OrderBy(m => m.StartDate).Select(m => m.StartDate).First();
                                var endDate = section.Meetings.OrderByDescending(m => m.EndDate).Select(m => m.EndDate).First();
                                dbSection.StartDate = startDate;
                                dbSection.EndDate = endDate;
                            }
                            updatedSections.Add(dbSection);
                        }
                    } else
                    {
                        // Section isn't cached. Create a new one.
                        dbSection = new Section()
                        {
                            SectionId = Guid.NewGuid(),
                            CRN = section.Crn,
                            ClassId = (Guid)dbClassId,
                            Meetings = new List<Meeting>(),
                            Type = section.Type,
                            Capacity = section.Capacity,
                            Enrolled = section.Enrolled,
                            RemainingSpace = section.RemainingSpace,
                            WaitlistCapacity = section.WaitlistCapacity,
                            WaitlistCount = section.WaitlistCount,
                            WaitlistSpace = section.WaitlistSpace
                        };

                        foreach (var meeting in section.Meetings)
                        {
                            // make sure instructors exist
                            var instructors = new List<Instructor>();
                            foreach (var inst in meeting.Instructors)
                            {
                                var instructorId = await EnsureInstructor(inst.Item2, inst.Item1);
                                if (!instructorEntities.ContainsKey(instructorId))
                                {
                                    instructorEntities[instructorId] = new Instructor()
                                    {
                                        InstructorId = instructorId
                                    };
                                }
                                instructors.Add(instructorEntities[instructorId]);
                            }

                            // make sure the building exists
                            var dbBuildingId = await EnsureBuilding((Guid)dbCampusId, meeting.BuildingCode, meeting.BuildingName);

                            // make sure the room exists
                            var dbRoomId = await EnsureRoom(dbBuildingId, meeting.RoomNumber);

                            var newMeeting = new Meeting()
                            {
                                MeetingId = Guid.NewGuid(),
                                Type = meeting.Type,
                                StartTime = meeting.StartTime,
                                Duration = meeting.EndTime.Subtract(meeting.StartTime),
                                Instructors = instructors,
                                RoomId = dbRoomId,
                                DaysOfWeek = meeting.DaysOfWeek,
                                Section = dbSection,
                                StartDate = meeting.StartDate,
                                EndDate = meeting.EndDate
                            };
                            dbSection.Meetings.Add(newMeeting);
                        }

                        if (section.Meetings.Count > 0)
                        {
                            var startDate = section.Meetings.OrderBy(m => m.StartDate).Select(m => m.StartDate).First();
                            var endDate = section.Meetings.OrderByDescending(m => m.EndDate).Select(m => m.EndDate).First();
                            dbSection.StartDate = startDate;
                            dbSection.EndDate = endDate;
                        }
                        newSections.Add(dbSection);
                    }
                }
                using (var db = new ApplicationDbContext())
                {
                    Console.WriteLine("\t\t\t{0} inserted / {1} updated sections", newSections.Count, updatedSections.Count);
                    // Attach all instructors as unchanged
                    foreach (var i in instructorEntities.Values)
                    {
                        db.Instructors.Attach(i);
                        db.Entry(i).State = EntityState.Unchanged;
                    }
                    foreach (var s in updatedSections)
                    {
                        //db.Sections.Attach(s);
                        db.Entry(s).State = EntityState.Modified;
                    }
                    foreach (var s in newSections)
                    {
                        db.Sections.Add(s);
                    }
                    await db.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// A simple class to assist with resolving links
        /// </summary>
        class LinkSections
        {
            public List<string> Links;
            public List<MyPurdueSection> Sections;

            public LinkSections(string link1, string link2, MyPurdueSection section)
            {
                Links = new List<string>()
                {
                    link1,
                    link2
                };
                Sections = new List<MyPurdueSection>()
                {
                    section
                };
            }

            public void Add(MyPurdueSection section)
            {
                if (!Links.Contains(section.LinkSelf))
                {
                    Links.Add(section.LinkSelf);
                }
                if (!Links.Contains(section.LinkOther))
                {
                    Links.Add(section.LinkOther);
                }
                Sections.Add(section);
            }

            public void Absorb(LinkSections other)
            {
                foreach (var l in other.Links)
                {
                    if (!Links.Contains(l))
                    {
                        Links.Add(l);
                    }
                }
                Sections.AddRange(other.Sections);
            }
        }

        /// <summary>
        /// Takes a flat list of MyPurdueSection objects and groups them together based on their LinkSelf and LinkOther values.
        /// </summary>
        /// <param name="sections">Flat list of MyPurdueSection objects</param>
        /// <returns>Groups of sections to be processed into "classes"</returns>
        private List<List<MyPurdueSection>> GroupSections(List<MyPurdueSection> sections)
        {
            var sectionsByLinks = new Dictionary<Tuple<string, string>, LinkSections>();
            var independentSections = new List<MyPurdueSection>();
            foreach (var section in sections)
            {
                if (section.LinkSelf == "" && section.LinkOther == "")
                {
                    independentSections.Add(section);
                    continue;
                }
                var linkSelfTuple = new Tuple<string, string>(section.LinkSelf, section.Number);
                var linkOtherTuple = new Tuple<string, string>(section.LinkOther, section.Number);
                LinkSections linkSelfGroups = null;
                sectionsByLinks.TryGetValue(linkSelfTuple, out linkSelfGroups);
                LinkSections linkOtherGroups = null;
                sectionsByLinks.TryGetValue(linkOtherTuple, out linkOtherGroups);
                if (linkSelfGroups == null && linkOtherGroups == null)
                {
                    var group = new LinkSections(section.LinkSelf, section.LinkOther, section);
                    sectionsByLinks[linkSelfTuple] = group;
                    sectionsByLinks[linkOtherTuple] = group;
                }
                else if (linkSelfGroups != null && linkOtherGroups == null)
                {
                    linkSelfGroups.Add(section);
                    sectionsByLinks[linkOtherTuple] = linkSelfGroups;
                }
                else if (linkSelfGroups == null && linkOtherGroups != null)
                {
                    linkOtherGroups.Add(section);
                    sectionsByLinks[linkSelfTuple] = linkOtherGroups;
                }
                else if (linkSelfGroups != null && linkOtherGroups != null)
                {
                    if (linkSelfGroups != linkOtherGroups)
                    {
                        linkSelfGroups.Absorb(linkOtherGroups);
                        foreach (var l in linkSelfGroups.Links)
                        {
                            sectionsByLinks[new Tuple<string, string>(l, section.Number)] = linkSelfGroups;
                        }
                    }
                    linkSelfGroups.Add(section);
                }
            }
            var sectionGroups = sectionsByLinks.Values.Distinct().Select(s => s.Sections).ToList();
            sectionGroups.AddRange(independentSections.Select(x => new List<MyPurdueSection>() { x }));
            return sectionGroups;
        }

        /// <summary>
        /// Searches the cache/database for the specified campus, inserting it if it does not exist.
        /// </summary>
        /// <param name="campusCode">Campus code as specified by MyPurdue</param>
        /// <param name="campusName">Campus name</param>
        /// <returns>GUID of Campus in database</returns>
        private async Task<Guid> EnsureCampus(string campusCode, string campusName)
        {
            using (var db = new ApplicationDbContext())
            {
                if (CampusCache.ContainsKey(campusCode.ToUpper()))
                {
                    return CampusCache[campusCode.ToUpper()];
                }
                Campus dbCampus = db.Campuses.AsNoTracking().SingleOrDefault(c => c.Code.ToUpper() == campusCode.ToUpper());
                if (dbCampus == null)
                {
                    dbCampus = new Campus()
                    {
                        CampusId = Guid.NewGuid(),
                        Code = campusCode,
                        Name = campusName,
                        Buildings = new List<Building>(),
                        ZipCode = ""
                    };
                    db.Campuses.Add(dbCampus);
                    await db.SaveChangesAsync();
                }
                CampusCache[campusCode.ToUpper()] = dbCampus.CampusId;
                return dbCampus.CampusId;
            }
        }

        /// <summary>
        /// Searches the cache/database for the specified subject, inserting it if it does not exist.
        /// </summary>
        /// <param name="subjectAbbreviation">Subject abbreviation</param>
        /// <param name="subjectName">Full subject name</param>
        /// <returns>The Subject GUID</returns>
        private async Task<Guid> EnsureSubject(string subjectAbbreviation, string subjectName)
        {
            if (SubjectCache.ContainsKey(subjectAbbreviation.ToUpper()))
            {
                return SubjectCache[subjectAbbreviation.ToUpper()];
            }
            using (var db = new ApplicationDbContext())
            {
                Subject dbSubject = db.Subjects.AsNoTracking().SingleOrDefault(s => s.Abbreviation.ToUpper() == subjectAbbreviation.ToUpper());
                if (dbSubject == null)
                {
                    dbSubject = new Subject()
                    {
                        SubjectId = Guid.NewGuid(),
                        Abbreviation = subjectAbbreviation.ToUpper(),
                        Name = subjectName,
                        Courses = new List<Course>()
                    };
                    db.Subjects.Add(dbSubject);
                    await db.SaveChangesAsync();
                }
                SubjectCache[subjectAbbreviation.ToUpper()] = dbSubject.SubjectId;
                return dbSubject.SubjectId;
            }
        }

        /// <summary>
        /// Searches the cache/database for the specified instructor, inserting it if it does not exist.
        /// </summary>
        /// <param name="email">Instructor email</param>
        /// <param name="name">Instructor name</param>
        /// <returns>The Instructor GUID</returns>
        private async Task<Guid> EnsureInstructor(string email, string name)
        {
            if (InstructorCache.ContainsKey(email.ToUpper()))
            {
                return InstructorCache[email.ToUpper()];
            }
            using (var db = new ApplicationDbContext())
            {
                Instructor dbInstructor = db.Instructors.AsNoTracking().SingleOrDefault(i => i.Email.ToUpper() == email.ToUpper());
                if (dbInstructor == null)
                {
                    dbInstructor = new Instructor()
                    {
                        InstructorId = Guid.NewGuid(),
                        Email = email,
                        Name = name,
                        Meetings = new List<Meeting>()
                    };
                    db.Instructors.Add(dbInstructor);
                    await db.SaveChangesAsync();
                }
                InstructorCache[email.ToUpper()] = dbInstructor.InstructorId;
                return dbInstructor.InstructorId;
            }
        }

        /// <summary>
        /// Searches the cache/database for the specified building, inserting it if it does not exist.
        /// </summary>
        /// <param name="campusId">GUID of the building's campus</param>
        /// <param name="buildingCode">Short code of the building</param>
        /// <param name="buildingName">Building name</param>
        /// <returns>The Building GUID</returns>
        private async Task<Guid> EnsureBuilding(Guid campusId, string buildingCode, string buildingName)
        {
            var buildingKey = new Tuple<Guid, string>(campusId, buildingCode.ToUpper());
            if (BuildingCache.ContainsKey(buildingKey))
            {
                return BuildingCache[buildingKey];
            }
            using (var db = new ApplicationDbContext())
            {
                Building dbBuilding = db.Buildings.AsNoTracking().SingleOrDefault(b => b.CampusId == campusId && b.ShortCode.ToUpper() == buildingCode.ToUpper());
                if (dbBuilding == null)
                {
                    dbBuilding = new Building()
                    {
                        BuildingId = Guid.NewGuid(),
                        CampusId = campusId,
                        ShortCode = buildingCode,
                        Name = buildingName,
                        Rooms = new List<Room>()
                    };
                    db.Buildings.Add(dbBuilding);
                    await db.SaveChangesAsync();
                }
                BuildingCache[buildingKey] = dbBuilding.BuildingId;
                return dbBuilding.BuildingId;
            }
        }

        /// <summary>
        /// Searches the cache/database for the specified room, inserting it if it does not exist.
        /// </summary>
        /// <param name="buildingId">GUID of the room's building</param>
        /// <param name="roomNumber">Room number</param>
        /// <returns>The Room GUID</returns>
        private async Task<Guid> EnsureRoom(Guid buildingId, string roomNumber)
        {
            var roomKey = new Tuple<Guid, string>(buildingId, roomNumber.ToUpper());
            if (RoomCache.ContainsKey(roomKey))
            {
                return RoomCache[roomKey];
            }
            using (var db = new ApplicationDbContext())
            {
                Room dbRoom = db.Rooms.AsNoTracking().SingleOrDefault(r => r.BuildingId == buildingId && r.Number.ToUpper() == roomNumber.ToUpper());
                if (dbRoom == null)
                {
                    dbRoom = new Room()
                    {
                        RoomId = Guid.NewGuid(),
                        BuildingId = buildingId,
                        Number = roomNumber,
                        Meetings = new List<Meeting>()
                    };
                    db.Rooms.Add(dbRoom);
                    await db.SaveChangesAsync();
                }
                RoomCache[roomKey] = dbRoom.RoomId;
                return dbRoom.RoomId;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                CampusCache = null;
                SubjectCache = null;
                InstructorCache = null;
                BuildingCache = null;
                RoomCache = null;
                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
