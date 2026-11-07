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
using static PurdueIo.Tests.TestResources;

namespace PurdueIo.Tests
{
    // An HttpMessageHandler that mimics the slice of the MyPurdue class search API
    // that MyPurdueConnection talks to. It never touches the network.
    //
    // By default it serves the recorded 202720 CS responses embedded in the test assembly
    // (see TestResources). Tests change what it serves through the public members below;
    // anything it hasn't been given gets a 404 that says what was missing.
    public class FakeMyPurdueHandler : HttpMessageHandler
    {
        // MyPurdue's page size for section searches (see MyPurdueConnection).
        private const int PageSize = 500;

        // Every request, as (HTTP method, path + query, request body), for wiring assertions.
        public List<(string Method, string PathAndQuery, string Body)> Requests { get; } = [];

        // Every section search, in request order.
        public List<(string Term, string Subject, int Offset)> SectionSearches { get; } = [];

        // The terms whose subject list was requested, in request order.
        public List<string> SubjectListTerms { get; } = [];

        // Served by classSearch/getTerms.
        public List<(string Code, string Name)> Terms { get; set; } =
            [(RecordedTerm, RecordedTermName)];

        // Served by classSearch/get_subject, for every term.
        public List<(string Code, string Name)> Subjects { get; set; } =
            [(RecordedSubject, RecordedSubjectName)];

        // Section search (searchResults/searchResults) responses, keyed by
        // (txt_term, txt_subject) and then by pageOffset. Starts with the recorded search.
        // Most tests use SetSections; edit pages directly to simulate bad responses.
        public Dictionary<(string Term, string Subject), Dictionary<int, string>>
            SectionSearchPages { get; } = new()
            {
                [(RecordedTerm, RecordedSubject)] = new()
                {
                    [0] = ReadText(SectionPage0),
                    [500] = ReadText(SectionPage500),
                },
            };

        // fetchLinkedSections responses keyed by the queried CRN. Starts with the verbatim
        // responses recorded while running the real GroupLinkedSectionsAsync heuristic, so
        // a heuristic change that queries an unrecorded CRN fails loudly.
        public Dictionary<string, string> LinkedSections => linkedSections ??=
            JsonSerializer
                .Deserialize<Dictionary<string, JsonElement>>(ReadText(LinkedSectionsFixture))
                .ToDictionary(kv => kv.Key, kv => kv.Value.GetRawText());
        private Dictionary<string, string> linkedSections;

        // The pages (by pageOffset) for one term/subject search, created empty if missing.
        public Dictionary<int, string> SectionSearchPagesFor(string term, string subject)
        {
            if (!SectionSearchPages.TryGetValue((term, subject), out var pages))
            {
                SectionSearchPages[(term, subject)] = pages = [];
            }
            return pages;
        }

        // Makes `sections` the complete result of a term/subject search, paginated like
        // the live service. Replaces anything previously served for that search.
        public void SetSections(string term, string subject, params MyPurdueSection[] sections)
        {
            var pages = SectionSearchPagesFor(term, subject);
            pages.Clear();
            pages[0] = SyntheticSections.Page(sections.Length, sections.Take(PageSize).ToArray());
            for (var offset = PageSize; offset < sections.Length; offset += PageSize)
            {
                pages[offset] = SyntheticSections.Page(sections.Length,
                    sections.Skip(offset).Take(PageSize).ToArray());
            }
        }

        // Makes fetchLinkedSections report `group` as one class when queried for any of
        // its members.
        public void SetLinkedSections(params MyPurdueSection[] group)
        {
            var response = JsonSerializer.Serialize(new MyPurdueLinkedSectionsResponse
            {
                SectionGroups = [group.ToList()],
            });
            foreach (var section in group)
            {
                LinkedSections[section.Crn] = response;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.Method.Method, request.RequestUri.PathAndQuery, body));

            var path = request.RequestUri.AbsolutePath;
            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            if (path.EndsWith("/classSearch/getTerms"))
            {
                return Json(JsonSerializer.Serialize(
                    Terms.Select(t => new { code = t.Code, description = t.Name })));
            }
            if (path.EndsWith("/classSearch/get_subject"))
            {
                SubjectListTerms.Add(query["term"]);
                // MyPurdue prefixes the name with the code; MyPurdueConnection strips it.
                return Json(JsonSerializer.Serialize(Subjects.Select(s =>
                    new { code = s.Code, description = $"{s.Code}-{s.Name}" })));
            }
            if (path.EndsWith("/term/search") || path.EndsWith("/classSearch/resetDataForm"))
            {
                // Session registration and search reset just have to succeed.
                return Json("{}");
            }
            if (path.EndsWith("/searchResults/searchResults"))
            {
                var (term, subject) = (query["txt_term"], query["txt_subject"]);
                var offset = int.Parse(query["pageOffset"]);
                SectionSearches.Add((term, subject, offset));
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
                var crn = query["courseReferenceNumber"];
                return LinkedSections.TryGetValue(crn, out var response)
                    ? Json(response)
                    : Error(
                        $"FakeMyPurdueHandler has no fetchLinkedSections response for CRN " +
                        $"{crn}. If this came from the recorded fixture, the grouping " +
                        "heuristic requested a section that was never captured - re-record " +
                        "the linked-sections fixture.");
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
