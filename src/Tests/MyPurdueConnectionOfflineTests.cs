using System;
using System.Collections.Generic;
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
        public async Task GroupLinkedSectionsReconstructsRecordedClassesExactly()
        {
            var (connection, handler) = CreateConnection();
            var sections = await connection.GetSectionsAsync(Term, Subject);

            var groups = await connection.GroupLinkedSectionsAsync(Term, sections);

            // The replay must use exactly the recorded query set: the grouping
            // heuristic (not per-CRN lookups) is what keeps this fast.
            var recorded = TestResources.LoadLinkedSections();
            var fetchRequests = handler.Requests
                .Where(r => r.PathAndQuery.Contains("searchResults/fetchLinkedSections"))
                .ToList();
            Assert.Equal(recorded.Count, fetchRequests.Count);

            // Ground truth: connected components over every recorded class's members
            // (the queried section counts as a member even when the response omits it),
            // restricted to this search; unreferenced (unlinked) sections are singletons.
            var parent = new Dictionary<string, string>();
            string Find(string x)
            {
                if (!parent.TryGetValue(x, out var p))
                {
                    parent[x] = x;
                    return x;
                }
                return p == x ? x : parent[x] = Find(p);
            }
            foreach (var (queriedCrn, response) in recorded)
            {
                Find(queriedCrn);
                foreach (var classmate in response.SectionGroups.SelectMany(g => g))
                {
                    var (ra, rb) = (Find(queriedCrn), Find(classmate.Crn));
                    if (ra != rb) parent[ra] = rb;
                }
            }
            var expectedParts = new Dictionary<string, HashSet<string>>();
            foreach (var crn in sections.Select(s => s.Crn))
            {
                if (!parent.ContainsKey(crn))
                {
                    expectedParts[crn] = [crn];
                    continue;
                }
                var root = Find(crn);
                if (!expectedParts.TryGetValue(root, out var part))
                {
                    expectedParts[root] = part = [];
                }
                part.Add(crn);
            }

            // The grouping algorithm must reproduce that partition exactly.
            var expected = expectedParts.Values
                .Select(p => string.Join("|", p.OrderBy(c => c))).OrderBy(k => k).ToList();
            var actual = groups
                .Select(g => string.Join("|", g.Select(s => s.Crn).OrderBy(c => c)))
                .OrderBy(k => k).ToList();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task GroupLinkedSectionsJoinsLectureWithItsLabs()
        {
            var (connection, _) = CreateConnection();
            var sections = await connection.GetSectionsAsync(Term, Subject);

            var groups = await connection.GroupLinkedSectionsAsync(Term, sections);

            // CRN 10787 is a CS 15900 laboratory (linkIdentifier AF). The real
            // fetchLinkedSections response for CRN 10786 puts it in the same class as
            // the lecture and other labs - something grouping on linkIdentifier alone
            // could never discover, since every member of bucket AF is another lab.
            var group = Assert.Single(groups, g => g.Any(s => s.Crn == "10787"));
            var scheduleTypes = group.Select(s => s.SectionType).ToList();
            Assert.Contains("Lecture", scheduleTypes);
            Assert.Contains("Laboratory", scheduleTypes);
        }
    }
}
