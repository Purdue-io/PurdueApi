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

        // Canned responses for searchResults/searchResults keyed by the requested
        // pageOffset. Individual tests replace entries to simulate bad or truncated
        // responses.
        public Dictionary<int, string> SearchPages { get; } = new()
        {
            [0] = TestResources.ReadText(TestResources.SectionPage0),
            [500] = TestResources.ReadText(TestResources.SectionPage500),
        };

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
