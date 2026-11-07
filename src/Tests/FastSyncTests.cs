using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PurdueIo.CatalogSync;
using PurdueIo.Database;
using PurdueIo.Database.Models;
using PurdueIo.Scraper.Connections;
using PurdueIo.Scraper.Connections.Models;
using Xunit;
using static PurdueIo.CatalogSync.FastSync;
using static PurdueIo.Tests.SyntheticSections;
using static PurdueIo.Tests.TestResources;

namespace PurdueIo.Tests
{
    // Runs the real FastSync against FakeMyPurdueHandler and an in-memory SQLite database
    // created by the real SQLite migration. Each test gets its own database.
    public sealed class FastSyncTests : IDisposable
    {
        // Foreign keys must be on: FastSync deletes sections without loading their
        // meetings and relies on the database to cascade those deletes.
        private readonly SqliteConnection connection =
            new("DataSource=:memory:;Foreign Keys=True");

        public FastSyncTests()
        {
            connection.Open();
            using var ctx = NewContext();
            ctx.Database.Migrate();
        }

        public void Dispose() => connection.Dispose();

        private ApplicationDbContext NewContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection, o => o.MigrationsAssembly("Database.Migrations.Sqlite"))
                .Options);

        // One sync "run" that must succeed. FastSync isolates failures instead of throwing,
        // so without this check a broken run could leave the DB unchanged and still "pass".
        private async Task<SyncResult> SyncAsync(FakeMyPurdueHandler handler,
            TermSyncBehavior behavior = TermSyncBehavior.SyncAllTerms,
            string[] terms = null, string[] subjects = null)
        {
            var result = await SyncAllowingFailuresAsync(handler, behavior, terms, subjects);
            Assert.True(result.Succeeded, "Sync failed: " + string.Join("; ",
                result.Failures.Select(f => $"{f.Description}: {f.Exception.Message}")));
            return result;
        }

        // One sync "run", mirroring CatalogSync/Program.cs: fresh connection and context.
        private async Task<SyncResult> SyncAllowingFailuresAsync(FakeMyPurdueHandler handler,
            TermSyncBehavior behavior = TermSyncBehavior.SyncAllTerms,
            string[] terms = null, string[] subjects = null)
        {
            var myPurdue = new MyPurdueConnection(
                NullLogger<MyPurdueConnection>.Instance, handler);
            using var ctx = NewContext();
            return await FastSync.SynchronizeAsync(myPurdue, ctx, NullLogger<FastSync>.Instance,
                termCodes: terms, subjectCodes: subjects, termSyncBehavior: behavior);
        }

        // A subject list for FakeMyPurdueHandler.Subjects where only the codes matter.
        private static List<(string Code, string Name)> SubjectList(params string[] codes) =>
            [.. codes.Select(c => (c, $"Subject {c}"))];

        // Distinct subjects the sync searched sections for, in request order.
        private static List<string> SearchedSubjects(FakeMyPurdueHandler handler) =>
            handler.SectionSearches.Select(s => s.Subject).Distinct().ToList();

        private static List<MyPurdueSection> RecordedSections() =>
            LoadSectionPage(SectionPage0).Sections
                .Concat(LoadSectionPage(SectionPage500).Sections)
                .ToList();

        private static DateOnly ParseDate(string value) =>
            DateOnly.ParseExact(value, "MM/dd/yyyy", CultureInfo.InvariantCulture);

        // Every row (and its identifying columns) of every synced table, as sorted strings,
        // so two snapshots can be compared for "nothing changed".
        private Dictionary<string, List<string>> Snapshot()
        {
            using var ctx = NewContext();
            static List<string> Sorted(IEnumerable<string> rows) =>
                rows.OrderBy(r => r, StringComparer.Ordinal).ToList();
            return new()
            {
                ["Terms"] = Sorted(ctx.Terms.ToList()
                    .Select(t => $"{t.Id}|{t.Code}|{t.Name}|{t.StartDate}|{t.EndDate}")),
                ["Subjects"] = Sorted(ctx.Subjects.ToList()
                    .Select(s => $"{s.Id}|{s.Abbreviation}|{s.Name}")),
                ["Campuses"] = Sorted(ctx.Campuses.ToList()
                    .Select(c => $"{c.Id}|{c.Code}|{c.Name}")),
                ["Buildings"] = Sorted(ctx.Buildings.ToList()
                    .Select(b => $"{b.Id}|{b.CampusId}|{b.ShortCode}|{b.Name}")),
                ["Rooms"] = Sorted(ctx.Rooms.ToList()
                    .Select(r => $"{r.Id}|{r.BuildingId}|{r.Number}")),
                ["Courses"] = Sorted(ctx.Courses.ToList()
                    .Select(c => $"{c.Id}|{c.SubjectId}|{c.Number}|{c.Title}|{c.CreditHours}")),
                ["Classes"] = Sorted(ctx.Classes.ToList()
                    .Select(c => $"{c.Id}|{c.CourseId}|{c.TermId}|{c.CampusId}")),
                ["Sections"] = Sorted(ctx.Sections.ToList()
                    .Select(s => $"{s.Id}|{s.Crn}|{s.ClassId}|{s.Type}|{s.StartDate}|{s.EndDate}")),
                ["Meetings"] = Sorted(ctx.Meetings.ToList()
                    .Select(m => $"{m.Id}|{m.SectionId}|{m.RoomId}|{m.DaysOfWeek}|" +
                        $"{m.StartTime}|{m.Duration}|{m.StartDate}|{m.EndDate}|{m.Type}")),
                ["Instructors"] = Sorted(ctx.Instructors.ToList()
                    .Select(i => $"{i.Id}|{i.Email}|{i.Name}")),
                ["MeetingInstructors"] = Sorted(ctx.Set<MeetingInstructor>().ToList()
                    .Select(mi => $"{mi.MeetingId}|{mi.InstructorId}")),
            };
        }

        private static void AssertSnapshotsEqual(
            Dictionary<string, List<string>> expected, Dictionary<string, List<string>> actual)
        {
            Assert.Equal(expected.Keys, actual.Keys);
            foreach (var table in expected.Keys)
            {
                Assert.True(expected[table].SequenceEqual(actual[table]),
                    $"Table {table} changed: " +
                    $"removed [{string.Join(", ", expected[table].Except(actual[table]))}], " +
                    $"added [{string.Join(", ", actual[table].Except(expected[table]))}]");
            }
        }

        // ---------------------------------------------------------------------------
        // A. Recorded fixture: 202720 CS, 589 sections
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task FullSyncPersistsEveryScrapedSection()
        {
            await SyncAsync(new FakeMyPurdueHandler());

            var scraped = RecordedSections();
            using var ctx = NewContext();

            // Every scraped CRN becomes exactly one section.
            Assert.Equal(
                scraped.Select(s => s.Crn).OrderBy(c => c),
                ctx.Sections.Select(s => s.Crn).ToList().OrderBy(c => c));

            // One class per linked-section group (the grouping itself is covered by
            // MyPurdueConnectionOfflineTests).
            var grouping = new MyPurdueConnection(
                NullLogger<MyPurdueConnection>.Instance, new FakeMyPurdueHandler());
            var groups = await grouping.GroupLinkedSectionsAsync(RecordedTerm,
                await grouping.GetSectionsAsync(RecordedTerm, RecordedSubject));
            Assert.Equal(groups.Count, ctx.Classes.Count());
            Assert.All(ctx.Classes.Select(c => c.Sections.Count).ToList(),
                n => Assert.True(n > 0));

            // Each section gets one meeting per scraped meeting time.
            var expectedMeetings = scraped.ToDictionary(s => s.Crn,
                s => (s.Meetings ?? []).Count(m => m.MeetingTime != null));
            var actualMeetings = ctx.Sections
                .Select(s => new { s.Crn, Count = s.Meetings.Count })
                .ToDictionary(s => s.Crn, s => s.Count);
            Assert.Equal(expectedMeetings.OrderBy(kv => kv.Key),
                actualMeetings.OrderBy(kv => kv.Key));

            var term = Assert.Single(ctx.Terms);
            Assert.Equal((RecordedTerm, RecordedTermName), (term.Code, term.Name));
            // The fake serves MyPurdue's "CS-Computer Sciences"; the prefix is stripped.
            var subject = Assert.Single(ctx.Subjects);
            Assert.Equal((RecordedSubject, RecordedSubjectName),
                (subject.Abbreviation, subject.Name));

            // Instructors are de-duplicated by (trimmed) e-mail.
            var expectedEmails = scraped
                .SelectMany(s => s.Faculty ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f.Email))
                .Select(f => f.Email.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(e => e, StringComparer.Ordinal);
            Assert.Equal(expectedEmails,
                ctx.Instructors.Select(i => i.Email).ToList()
                    .OrderBy(e => e, StringComparer.Ordinal));
        }

        [Fact]
        public async Task TermDatesSpanSectionDates()
        {
            await SyncAsync(new FakeMyPurdueHandler());

            var meetingTimes = RecordedSections()
                .SelectMany(s => s.Meetings ?? [])
                .Select(m => m.MeetingTime)
                .Where(t => t != null)
                .ToList();
            using var ctx = NewContext();
            var term = Assert.Single(ctx.Terms);
            Assert.Equal(meetingTimes.Min(t => ParseDate(t.StartDate)), term.StartDate);
            Assert.Equal(meetingTimes.Max(t => ParseDate(t.EndDate)), term.EndDate);
        }

        [Fact]
        public async Task ResyncIsIdempotent()
        {
            // API consumers hold on to IDs, so an unchanged catalog must not churn rows.
            await SyncAsync(new FakeMyPurdueHandler());
            var first = Snapshot();

            await SyncAsync(new FakeMyPurdueHandler());

            AssertSnapshotsEqual(first, Snapshot());
        }

        // ---------------------------------------------------------------------------
        // B. Synthetic scenarios
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task RemovedSectionIsDeletedWithMeetingsAndEmptyClass()
        {
            var keep = Section("10001");
            var drop = Section("10002", courseNumber: "25100", title: "Data Structures",
                meetings: [Meeting(building: "WALC", room: "1055", days: "TR")],
                faculty: [Faculty("Grace Hopper", "ghopper@purdue.edu")]);
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject, keep, drop);
            await SyncAsync(handler);

            Guid droppedSectionId, droppedClassId;
            using (var ctx = NewContext())
            {
                var dropped = ctx.Sections.Single(s => s.Crn == "10002");
                (droppedSectionId, droppedClassId) = (dropped.Id, dropped.ClassId);
            }
            var before = Snapshot();

            handler.SetSections(DefaultTerm, DefaultSubject, keep);
            await SyncAsync(handler);

            using (var ctx = NewContext())
            {
                Assert.Equal("10001", Assert.Single(ctx.Sections).Crn);
                Assert.False(ctx.Classes.Any(c => c.Id == droppedClassId));
                Assert.False(ctx.Meetings.Any(m => m.SectionId == droppedSectionId));
                var hopper = ctx.Instructors.Single(i => i.Email == "ghopper@purdue.edu");
                Assert.False(ctx.Set<MeetingInstructor>().Any(mi => mi.InstructorId == hopper.Id));
            }

            // The surviving section's rows are untouched.
            var after = Snapshot();
            foreach (var table in new[] { "Sections", "Meetings", "MeetingInstructors" })
            {
                Assert.Subset(before[table].ToHashSet(), after[table].ToHashSet());
            }
        }

        [Fact]
        public async Task MultipleRemovedSectionsAreDeletedTogether()
        {
            var handler = new FakeMyPurdueHandler();
            var survivor = Section("10002", courseNumber: "25100", title: "Data Structures");
            handler.SetSections(DefaultTerm, DefaultSubject,
                Section("10001"),
                survivor,
                Section("10003", courseNumber: "25200", title: "Systems Programming"));
            await SyncAsync(handler);

            handler.SetSections(DefaultTerm, DefaultSubject, survivor);
            await SyncAsync(handler);

            using var ctx = NewContext();
            Assert.Equal("10002", Assert.Single(ctx.Sections).Crn);
            Assert.Single(ctx.Classes);
            Assert.Single(ctx.Meetings);
        }

        [Fact]
        public async Task ChangedMeetingTimeReplacesOnlyThatMeeting()
        {
            var lecture = Meeting();
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001",
                meetings: [
                    lecture,
                    Meeting(begin: "1500", end: "1650", room: "B160", days: "T"),
                ]));
            await SyncAsync(handler);

            Guid lectureMeetingId, labMeetingId;
            using (var ctx = NewContext())
            {
                lectureMeetingId = ctx.Meetings
                    .Single(m => m.Duration == TimeSpan.FromMinutes(50)).Id;
                labMeetingId = ctx.Meetings
                    .Single(m => m.Duration == TimeSpan.FromMinutes(110)).Id;
            }

            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001",
                meetings: [
                    lecture,
                    Meeting(begin: "1600", end: "1750", room: "B160", days: "T"),
                ]));
            await SyncAsync(handler);

            using (var ctx = NewContext())
            {
                var meetings = ctx.Meetings.ToList();
                Assert.Equal(2, meetings.Count);
                Assert.Contains(meetings, m => m.Id == lectureMeetingId);
                Assert.DoesNotContain(meetings, m => m.Id == labMeetingId);
                Assert.Contains(meetings, m => m.StartTime == new TimeOnly(16, 0));
                // Instructor links follow the meetings: none left pointing at the old one.
                Assert.Equal(2, ctx.Set<MeetingInstructor>().Count());
            }
        }

        [Fact]
        public async Task InstructorChangesUpdateLinksButKeepInstructorRows()
        {
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001",
                faculty: [Faculty("Ada Lovelace", "alovelace@purdue.edu")]));
            await SyncAsync(handler);

            // Replace the instructor; the same e-mail also appears padded with whitespace.
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001", faculty: [
                Faculty("Alan Turing", "aturing@purdue.edu"),
                Faculty("Alan Turing", "  aturing@purdue.edu "),
            ]));
            await SyncAsync(handler);

            using var ctx = NewContext();
            var meeting = ctx.Meetings.Include(m => m.Instructors).Single();
            Assert.Equal("aturing@purdue.edu", Assert.Single(meeting.Instructors).Email);
            // Former instructors are not deleted.
            Assert.Equal(
                new[] { "alovelace@purdue.edu", "aturing@purdue.edu" },
                ctx.Instructors.Select(i => i.Email).OrderBy(e => e).ToArray());
        }

        [Fact]
        public async Task MeetingWithoutLocationUsesTbaRoom()
        {
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject,
                Section("10001", meetings: [Meeting(building: null)]),
                Section("10002", courseNumber: "25100", title: "Data Structures",
                    meetings: [Meeting(building: null, days: "TR")]));
            await SyncAsync(handler);

            using var ctx = NewContext();
            var building = Assert.Single(ctx.Buildings.Include(b => b.Rooms));
            Assert.Equal(("TBA", "TBA"), (building.ShortCode, building.Name));
            var room = Assert.Single(building.Rooms);
            Assert.Equal("TBA", room.Number);
            Assert.All(ctx.Meetings.ToList(), m => Assert.Equal(room.Id, m.RoomId));
        }

        [Fact]
        public async Task CreditHoursFallBackAndUpdate()
        {
            var handler = new FakeMyPurdueHandler();
            // Variable-credit sections report a range instead of creditHours.
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001", creditHours: null,
                creditHourLow: 1, creditHourHigh: 4));
            await SyncAsync(handler);
            using (var ctx = NewContext())
            {
                Assert.Equal(4, Assert.Single(ctx.Courses).CreditHours);
            }

            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001", creditHours: 3));
            await SyncAsync(handler);
            using (var ctx = NewContext())
            {
                Assert.Equal(3, Assert.Single(ctx.Courses).CreditHours);
            }
        }

        [Fact]
        public async Task LinkedSectionsShareOneClassAcrossResyncs()
        {
            var lecture = Section("10001", linkIdentifier: "A1");
            var lab = Section("10002", type: "Laboratory", linkIdentifier: "L1",
                meetings: [Meeting(begin: "1500", end: "1650", room: "B160", days: "T")]);
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject, lecture, lab);
            handler.SetLinkedSections(lecture, lab);

            await SyncAsync(handler);
            Guid classId;
            using (var ctx = NewContext())
            {
                var cls = Assert.Single(ctx.Classes.Include(c => c.Sections));
                Assert.Equal(new[] { "10001", "10002" },
                    cls.Sections.Select(s => s.Crn).OrderBy(c => c).ToArray());
                classId = cls.Id;
            }

            await SyncAsync(handler);
            using (var ctx = NewContext())
            {
                Assert.Equal(classId, Assert.Single(ctx.Classes).Id);
            }
        }

        [Fact]
        public async Task SectionsWithoutCourseInfoAreSkipped()
        {
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject,
                Section("10001"),
                Section("10002", courseNumber: "25100", title: ""));

            await SyncAsync(handler);

            using var ctx = NewContext();
            Assert.Equal("10001", Assert.Single(ctx.Sections).Crn);
            Assert.Single(ctx.Classes);
        }

        // ---------------------------------------------------------------------------
        // C. Term and subject selection
        // ---------------------------------------------------------------------------

        private const string OtherTerm = "202710";

        // The default term plus an earlier one with no sections configured.
        private static List<(string Code, string Name)> TwoTerms() =>
            [(DefaultTerm, RecordedTermName), (OtherTerm, "Fall 2026")];

        [Fact]
        public async Task TermFilterSyncsOnlyRequestedTerms()
        {
            var handler = new FakeMyPurdueHandler { Terms = TwoTerms() };
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"));

            await SyncAsync(handler, terms: [DefaultTerm]);

            using var ctx = NewContext();
            Assert.Equal(DefaultTerm, Assert.Single(ctx.Terms).Code);
            Assert.Equal([DefaultTerm], handler.SubjectListTerms);
            Assert.All(handler.SectionSearches, s => Assert.Equal(DefaultTerm, s.Term));
        }

        [Fact]
        public async Task SyncNewAndCurrentSkipsEndedTerms()
        {
            using (var ctx = NewContext())
            {
                ctx.Terms.Add(new Term
                {
                    Id = Guid.NewGuid(),
                    Code = OtherTerm,
                    Name = "Fall 2026",
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1).AddMonths(-4)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddYears(-1)),
                });
                ctx.SaveChanges();
            }
            var handler = new FakeMyPurdueHandler { Terms = TwoTerms() };
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"));

            await SyncAsync(handler, TermSyncBehavior.SyncNewAndCurrentTerms);

            Assert.Equal([DefaultTerm], handler.SubjectListTerms);
            using var check = NewContext();
            Assert.Equal("10001", Assert.Single(check.Sections).Crn);
        }

        [Fact]
        public async Task SubjectFilterSyncsOnlyRequestedSubjects()
        {
            var handler = new FakeMyPurdueHandler
            {
                Subjects = [(DefaultSubject, RecordedSubjectName), ("MA", "Mathematics")],
            };
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"));

            // Without the filter this would fail: MA has no sections configured.
            await SyncAsync(handler, subjects: [DefaultSubject]);

            Assert.Equal([DefaultSubject], SearchedSubjects(handler));
            using var ctx = NewContext();
            Assert.Equal(DefaultSubject, Assert.Single(ctx.Subjects).Abbreviation);
        }

        // ---------------------------------------------------------------------------
        // D. Failure safety and isolation
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task FailedScrapeLeavesExistingDataIntact()
        {
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"),
                Section("10002", courseNumber: "25100", title: "Data Structures"));
            await SyncAsync(handler);
            var before = Snapshot();

            // The service claims 2 results but stops after the first page.
            var pages = handler.SectionSearchPagesFor(DefaultTerm, DefaultSubject);
            pages[0] = Page(2, Section("10001"));
            pages[1] = Page(2);

            var result = await SyncAllowingFailuresAsync(handler);

            var failure = Assert.Single(result.Failures);
            Assert.Equal((DefaultTerm, DefaultSubject), (failure.TermCode, failure.SubjectCode));
            Assert.IsType<ApplicationException>(failure.Exception);
            AssertSnapshotsEqual(before, Snapshot());
        }

        [Fact]
        public async Task FailingSubjectDoesNotStopLaterSubjects()
        {
            // MA has no sections configured, so its search fails.
            var handler = new FakeMyPurdueHandler { Subjects = SubjectList("MA", DefaultSubject) };
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"));

            var result = await SyncAllowingFailuresAsync(handler);

            Assert.False(result.Succeeded);
            var failure = Assert.Single(result.Failures);
            Assert.Equal("MA", failure.SubjectCode);
            Assert.Contains("no section search pages", failure.Exception.Message);
            using var ctx = NewContext();
            Assert.Equal("10001", Assert.Single(ctx.Sections).Crn);
            // The failure started a fresh MyPurdue session for the next subject.
            Assert.Equal(2, handler.Requests.Count(r => r.PathAndQuery.Contains("/term/search")));
        }

        [Fact]
        public async Task FailedSubjectChangesAreNotSavedWithTheNextSubject()
        {
            var handler = new FakeMyPurdueHandler
            {
                Subjects = SubjectList(DefaultSubject, "MA", "EE"),
            };
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"));
            // MA's campus reuses CS's campus name under a different code, so its SaveChanges
            // violates the unique index on Campus.Name after entities are already tracked.
            handler.SetSections(DefaultTerm, "MA",
                Section("20001", courseNumber: "16100", title: "Calculus I", subject: "MA",
                    meetings: [Meeting(campusCode: "WL2")]));
            handler.SetSections(DefaultTerm, "EE",
                Section("30001", courseNumber: "20100", title: "Linear Circuits",
                    subject: "EE"));

            var result = await SyncAllowingFailuresAsync(handler);

            Assert.Equal("MA", Assert.Single(result.Failures).SubjectCode);
            Assert.IsType<DbUpdateException>(result.Failures[0].Exception);
            using var ctx = NewContext();
            Assert.Equal(new[] { "10001", "30001" },
                ctx.Sections.Select(s => s.Crn).OrderBy(c => c).ToArray());
            Assert.DoesNotContain(ctx.Campuses, c => c.Code == "WL2");
            Assert.DoesNotContain(ctx.Courses, c => c.Title == "Calculus I");
        }

        [Fact]
        public async Task ConsecutiveFailuresAbortTheRun()
        {
            // None of these subjects have sections configured, so every one fails.
            var subjects = Enumerable.Range(1, MAX_CONSECUTIVE_FAILURES + 2)
                .Select(i => $"X{i}").ToArray();
            var handler = new FakeMyPurdueHandler { Subjects = SubjectList(subjects) };

            var aborted = await Assert.ThrowsAsync<SyncAbortedException>(
                () => SyncAllowingFailuresAsync(handler));

            Assert.Equal(subjects.Take(MAX_CONSECUTIVE_FAILURES), SearchedSubjects(handler));
            // The exception carries every failure so far, so the caller can report them.
            Assert.Equal(
                subjects.Take(MAX_CONSECUTIVE_FAILURES).Select(s => $"{DefaultTerm} {s}"),
                aborted.Failures.Select(f => f.Description));
            Assert.Same(aborted.Failures[^1].Exception, aborted.InnerException);
        }

        [Fact]
        public async Task ConsecutiveFailuresCountAcrossTerms()
        {
            // Each term fails fewer than the limit, but together they exceed it.
            var perTerm = MAX_CONSECUTIVE_FAILURES / 2 + 1;
            var handler = new FakeMyPurdueHandler
            {
                Terms = TwoTerms(),
                Subjects = SubjectList([.. Enumerable.Range(1, perTerm).Select(i => $"X{i}")]),
            };

            await Assert.ThrowsAsync<SyncAbortedException>(
                () => SyncAllowingFailuresAsync(handler));
            Assert.Equal([DefaultTerm, OtherTerm], handler.SubjectListTerms);
        }

        [Fact]
        public async Task SuccessResetsTheConsecutiveFailureCount()
        {
            // Failing X subjects alternate with good G subjects, so the run never hits the
            // limit despite MAX_CONSECUTIVE_FAILURES failures in total.
            var subjects = Enumerable.Range(1, MAX_CONSECUTIVE_FAILURES)
                .SelectMany(i => new[] { $"X{i}", $"G{i}" }).ToList();
            var handler = new FakeMyPurdueHandler { Subjects = SubjectList([.. subjects]) };
            for (var i = 1; i <= MAX_CONSECUTIVE_FAILURES; i++)
            {
                handler.SetSections(DefaultTerm, $"G{i}", Section($"1100{i}", subject: $"G{i}"));
            }

            var result = await SyncAllowingFailuresAsync(handler);

            Assert.Equal(MAX_CONSECUTIVE_FAILURES, result.Failures.Count);
            Assert.Equal(subjects, SearchedSubjects(handler));
        }

        [Fact]
        public async Task EmptyScrapeCurrentlyDeletesSubjectSections()
        {
            var handler = new FakeMyPurdueHandler();
            handler.SetSections(DefaultTerm, DefaultSubject, Section("10001"),
                Section("10002", courseNumber: "25100", title: "Data Structures"));
            await SyncAsync(handler);

            // A "successful" empty response is indistinguishable from a subject with no
            // sections, so everything for the subject is deleted. Review item #5 proposes
            // a guard; flip this assertion when that lands.
            handler.SetSections(DefaultTerm, DefaultSubject);
            await SyncAsync(handler);

            using var ctx = NewContext();
            Assert.Empty(ctx.Sections);
            Assert.Empty(ctx.Classes);
            Assert.Empty(ctx.Meetings);
        }
    }
}
