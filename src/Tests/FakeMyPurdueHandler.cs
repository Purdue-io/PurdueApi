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

namespace PurdueIo.Tests
{
    // An HttpMessageHandler that mimics the slice of the MyPurdue class search API
    // that MyPurdueConnection talks to, backed by the cached JSON responses embedded
    // in the test assembly. It never touches the network.
    public class FakeMyPurdueHandler : HttpMessageHandler
    {
        // Recorded as (HTTP method, path + query, request body) for wiring assertions.
        public List<(string Method, string PathAndQuery, string Body)> Requests { get; } = [];

        // Term and subject of the recorded section search served by default.
        public const string RecordedTerm = "202720";
        public const string RecordedSubject = "CS";

        // Canned section search (searchResults/searchResults) pages, keyed by
        // (txt_term, txt_subject) and then by the requested pageOffset. Starts with the
        // recorded 202720 CS search; a search for any term/subject not listed here gets a
        // 404. Individual tests replace entries to simulate bad or truncated responses.
        public Dictionary<(string Term, string Subject), Dictionary<int, string>>
            SectionSearchPages { get; } = new()
            {
                [(RecordedTerm, RecordedSubject)] = new()
                {
                    [0] = TestResources.ReadText(TestResources.SectionPage0),
                    [500] = TestResources.ReadText(TestResources.SectionPage500),
                },
            };

        // The pages (by pageOffset) for one term/subject search, created empty if missing.
        public Dictionary<int, string> SectionSearchPagesFor(string term, string subject)
        {
            if (!SectionSearchPages.TryGetValue((term, subject), out var pages))
            {
                SectionSearchPages[(term, subject)] = pages = [];
            }
            return pages;
        }

        // fetchLinkedSections responses that take precedence over the recorded fixture,
        // keyed by queried CRN. Lets tests with synthetic sections define linked groups.
        public Dictionary<string, string> LinkedSectionOverrides { get; } = [];

        // Canned classSearch/getTerms response.
        public string TermsJson { get; set; } =
            "[{\"code\":\"202720\",\"description\":\"Spring 2027\"}]";

        // Canned classSearch/get_subject response. MyPurdue prefixes the name with the
        // code, which MyPurdueConnection strips.
        public string SubjectsJson { get; set; } =
            "[{\"code\":\"CS\",\"description\":\"CS-Computer Sciences\"}]";

        // Verbatim fetchLinkedSections responses captured from the live service while
        // running the real GroupLinkedSectionsAsync heuristic, keyed by the queried CRN.
        // Only those exact CRNs can be served: if the grouping heuristic ever queries
        // something else, that is a behavior change worth failing on.
        private Dictionary<string, string> linkedResponses;
        private Dictionary<string, string> LinkedResponses => linkedResponses ??=
            JsonSerializer
                .Deserialize<Dictionary<string, JsonElement>>(
                    TestResources.ReadText(TestResources.LinkedSectionsFixture))
                .ToDictionary(kv => kv.Key, kv => kv.Value.GetRawText());

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method.Method, request.RequestUri.PathAndQuery, body));

            var path = request.RequestUri.AbsolutePath;
            if (path.EndsWith("/classSearch/getTerms"))
            {
                return Json(TermsJson);
            }
            if (path.EndsWith("/classSearch/get_subject"))
            {
                return Json(SubjectsJson);
            }
            if (path.EndsWith("/term/search") || path.EndsWith("/classSearch/resetDataForm"))
            {
                // Session registration and search reset just have to succeed.
                return Json("{}");
            }
            if (path.EndsWith("/searchResults/searchResults"))
            {
                var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
                var (term, subject) = (query["txt_term"], query["txt_subject"]);
                var offset = int.Parse(query["pageOffset"]);
                if (!SectionSearchPages.TryGetValue((term, subject), out var pages))
                {
                    return Error("FakeMyPurdueHandler has no section search pages for " +
                        $"term {term}, subject {subject}");
                }
                return pages.TryGetValue(offset, out var page)
                    ? Json(page)
                    : Error("FakeMyPurdueHandler has no section search page for " +
                        $"term {term}, subject {subject}, pageOffset={offset}");
            }
            if (path.EndsWith("/searchResults/fetchLinkedSections"))
            {
                var crn = HttpUtility.ParseQueryString(request.RequestUri.Query)
                    ["courseReferenceNumber"];
                if (LinkedSectionOverrides.TryGetValue(crn, out var overridden))
                {
                    return Json(overridden);
                }
                return LinkedResponses.TryGetValue(crn, out var response)
                    ? Json(response)
                    : Error(
                        $"FakeMyPurdueHandler has no recorded fetchLinkedSections response " +
                        $"for CRN {crn}. The grouping heuristic requested a section that " +
                        "was never captured - re-record the linked-sections fixture.");
            }
            return Error($"FakeMyPurdueHandler received unexpected request: {path}");
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
