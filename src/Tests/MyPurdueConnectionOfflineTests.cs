using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using PurdueIo.Scraper.Connections;
using Xunit;

namespace PurdueIo.Tests
{
    // Runs MyPurdueConnection's real request/pagination logic against FakeMyPurdueHandler
    // (cached embedded responses), so the scraper wiring is tested without the network.
    public class MyPurdueConnectionOfflineTests
    {
        private const string Term = "202720";
        private const string Subject = "CS";

        private static (MyPurdueConnection Connection, FakeMyPurdueHandler Handler)
            CreateConnection()
        {
            var handler = new FakeMyPurdueHandler();
            var connection = new MyPurdueConnection(
                NullLogger<MyPurdueConnection>.Instance, handler);
            return (connection, handler);
        }

        [Fact]
        public async Task GetSectionsPaginatesUntilTotalCount()
        {
            var (connection, handler) = CreateConnection();

            var sections = await connection.GetSectionsAsync(Term, Subject);

            Assert.Equal(589, sections.Count);
            var searches = handler.Requests
                .Where(r => r.PathAndQuery.Contains("searchResults/searchResults")).ToList();
            Assert.Equal(2, searches.Count);
            Assert.Contains("pageOffset=0", searches[0].PathAndQuery);
            Assert.Contains("pageOffset=500", searches[1].PathAndQuery);
            Assert.All(searches, r =>
            {
                Assert.Contains($"txt_term={Term}", r.PathAndQuery);
                Assert.Contains($"txt_subject={Subject}", r.PathAndQuery);
                Assert.Contains("pageMaxSize=500", r.PathAndQuery);
            });
        }

        [Fact]
        public async Task GetSectionsEstablishesSessionOnce()
        {
            var (connection, handler) = CreateConnection();

            await connection.GetSectionsAsync(Term, Subject);

            var sessionPosts = handler.Requests
                .Where(r => r.Method == "POST" && r.PathAndQuery.Contains("term/search"))
                .ToList();
            Assert.Single(sessionPosts);
            Assert.Contains($"term={Term}", sessionPosts[0].Body);
            var sessionId = System.Web.HttpUtility
                .ParseQueryString(sessionPosts[0].Body)["uniqueSessionId"];
            Assert.False(string.IsNullOrEmpty(sessionId));

            Assert.Single(handler.Requests,
                r => r.PathAndQuery.Contains("classSearch/resetDataForm"));

            // Every page request must carry the session ID we registered.
            var searches = handler.Requests
                .Where(r => r.PathAndQuery.Contains("searchResults/searchResults")).ToList();
            Assert.NotEmpty(searches);
            Assert.All(searches, r => Assert.Equal(sessionId,
                System.Web.HttpUtility.ParseQueryString(r.PathAndQuery)["uniqueSessionId"]));
        }

        [Fact]
        public async Task GetSectionsThrowsWhenSuccessIsFalse()
        {
            var (connection, handler) = CreateConnection();
            handler.SearchPages[0] = "{\"success\":false,\"totalCount\":0,\"data\":[]}";

            var exception = await Assert.ThrowsAsync<ApplicationException>(
                () => connection.GetSectionsAsync(Term, Subject));
            Assert.Contains("`success` not set to true", exception.Message);
        }

        [Fact]
        public async Task GetSectionsThrowsOnWrongSubjectInResults()
        {
            var (connection, handler) = CreateConnection();
            handler.SearchPages[0] =
                "{\"success\":true,\"totalCount\":1,\"data\":[" +
                "{\"subject\":\"EE\",\"courseReferenceNumber\":\"99999\"}]}";

            var exception = await Assert.ThrowsAsync<ApplicationException>(
                () => connection.GetSectionsAsync(Term, Subject));
            Assert.Contains("EE", exception.Message);
            Assert.Contains("stale", exception.Message);
        }

        [Fact]
        public async Task GetSectionsThrowsOnEmptyPageBeforeTotalCount()
        {
            var (connection, handler) = CreateConnection();
            // Page 0 delivers 500 of 589; page 500 comes back empty (truncated scrape).
            handler.SearchPages[500] = "{\"success\":true,\"totalCount\":589,\"data\":[]}";

            var exception = await Assert.ThrowsAsync<ApplicationException>(
                () => connection.GetSectionsAsync(Term, Subject));
            Assert.Contains("stopped at 500 of 589", exception.Message);
        }

        [Fact]
        public async Task GroupLinkedSectionsGroupsLinkedSectionsByLinkIdentifier()
        {
            var (connection, handler) = CreateConnection();
            var sections = await connection.GetSectionsAsync(Term, Subject);

            var groups = await connection.GroupLinkedSectionsAsync(Term, sections);

            // The fake must actually have served link lookups.
            Assert.Contains(handler.Requests,
                r => r.PathAndQuery.Contains("searchResults/fetchLinkedSections"));

            var groupList = groups.ToList();

            // Count conservation: every input section lands in exactly one group.
            var grouped = groupList.SelectMany(g => g).ToList();
            Assert.Equal(sections.Count, grouped.Count);
            Assert.Equal(sections.Count, grouped.Select(s => s.Crn).Distinct().Count());

            // Every linked section shares its group with all sections that share its
            // (subjectCourse, linkIdentifier) key.
            var groupIndexOf = grouped
                .Select((s, i) => (s.Crn, GroupIndex: groupList.FindIndex(g => g.Any(x => x.Crn == s.Crn))))
                .ToDictionary(e => e.Crn, e => e.GroupIndex);
            var linkedKeyGroups = sections
                .Where(s => s.IsSectionLinked)
                .GroupBy(s => (s.SubjectCourseNumber, s.LinkIdentifier));
            foreach (var keyGroup in linkedKeyGroups)
            {
                Assert.Single(keyGroup.Select(s => groupIndexOf[s.Crn]).Distinct());
            }

            // Unlinked sections stay singletons.
            Assert.All(sections.Where(s => !s.IsSectionLinked), s =>
                Assert.Single(groups.Single(g => g.Any(x => x.Crn == s.Crn))));
        }
    }
}
