using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using PurdueIo.Scraper.Connections.Models;

namespace PurdueIo.Tests
{
    // An HttpMessageHandler that mimics the slice of the MyPurdue class search API
    // that MyPurdueConnection talks to, backed by the cached JSON responses embedded
    // in the test assembly. It never touches the network.
    public class FakeMyPurdueHandler : HttpMessageHandler
    {
        // Recorded as (HTTP method, path + query, request body) for wiring assertions.
        public List<(string Method, string PathAndQuery, string Body)> Requests { get; } = [];

        // Canned responses for searchResults/searchResults keyed by the requested
        // pageOffset. Individual tests replace entries to simulate bad or truncated
        // responses.
        public Dictionary<int, string> SearchPages { get; } = new()
        {
            [0] = TestResources.ReadText(TestResources.SectionPage0),
            [500] = TestResources.ReadText(TestResources.SectionPage500),
        };

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method.Method, request.RequestUri.PathAndQuery, body));

            var path = request.RequestUri.AbsolutePath;
            if (path.EndsWith("/term/search") || path.EndsWith("/classSearch/resetDataForm"))
            {
                // Session registration and search reset just have to succeed.
                return Json("{}");
            }
            if (path.EndsWith("/searchResults/searchResults"))
            {
                var offset = int.Parse(
                    HttpUtility.ParseQueryString(request.RequestUri.Query)["pageOffset"]);
                return SearchPages.TryGetValue(offset, out var page)
                    ? Json(page)
                    : Error($"FakeMyPurdueHandler has no canned page for pageOffset={offset}");
            }
            if (path.EndsWith("/searchResults/fetchLinkedSections"))
            {
                var crn = HttpUtility.ParseQueryString(request.RequestUri.Query)
                    ["courseReferenceNumber"];
                return Json(BuildLinkedSectionsResponse(crn));
            }
            return Error($"FakeMyPurdueHandler received unexpected request: {path}");
        }

        private List<MyPurdueSection> allSections;
        private List<MyPurdueSection> AllSections => allSections ??=
            [.. TestResources.LoadSectionPage(TestResources.SectionPage0).Sections,
             .. TestResources.LoadSectionPage(TestResources.SectionPage500).Sections];

        // The real endpoint answers with the whole link group containing the queried
        // CRN. Mimic that using the (subjectCourse, linkIdentifier) key that
        // MyPurdueConnection.GroupLinkedSectionsAsync itself groups on.
        private string BuildLinkedSectionsResponse(string crn)
        {
            var response = new MyPurdueLinkedSectionsResponse
            {
                SectionGroups = new List<ICollection<MyPurdueSection>>(),
            };
            var target = AllSections.FirstOrDefault(s => s.Crn == crn);
            if (target != null && target.IsSectionLinked)
            {
                response.SectionGroups.Add(AllSections
                    .Where(s => s.SubjectCourseNumber == target.SubjectCourseNumber
                        && s.LinkIdentifier == target.LinkIdentifier)
                    .ToList());
            }
            return JsonSerializer.Serialize(response);
        }

        private static HttpResponseMessage Json(string json) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

        private static HttpResponseMessage Error(string message) =>
            new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent(message, Encoding.UTF8, "text/plain"),
            };
    }
}
