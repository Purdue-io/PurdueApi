using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PurdueIo.Scraper.Connections.Models;

namespace PurdueIo.Scraper.Connections
{
    public class MyPurdueConnection
    {
        private readonly ILogger<MyPurdueConnection> logger;
        private readonly CookieContainer cookies = new();
        private readonly HttpClient httpClient;
        private const string BASE_URL =
            "https://apps.mypurdue.purdue.edu/StudentRegistrationSsb/ssb/";
        private (string sessionId, string sessionTerm)? myPurdueSession = null;

        // the service seems to enforce a 500-item limit before requiring pagination
        private const uint MAX_PAGINATION_COUNT = 500;

        private const string VIEW_ONLY_TAG = "(View Only)";

        public MyPurdueConnection(ILogger<MyPurdueConnection> logger)
        {
            this.logger = logger;

            var httpHandler = new HttpClientHandler()
            {
                CookieContainer = cookies, // MyPurdue stores a lot of state in cookies - we need
                                           // to persist them to avoid upsetting it
                AllowAutoRedirect = false, // We'll handle redirects by ourselves
            };
            httpClient = new HttpClient(httpHandler)
            {
                BaseAddress = new Uri(BASE_URL)
            };

            // Pretend we're Firefox
            httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (X11; Linux x86_64; rv:157.0) Gecko/20100101 Firefox/157.0");
        }

        public async Task<ICollection<MyPurdueTerm>> GetTermListAsync()
        {
            // TODO: If there are ever more than 500 terms, we'll need to implement code to pull
            // down the rest of them.
            var result = await httpClient.GetAsync(
                $"classSearch/getTerms?searchTerm=&offset=1&max={MAX_PAGINATION_COUNT}");
            if (result.IsSuccessStatusCode)
            {
                var terms = JsonSerializer.Deserialize<List<MyPurdueTerm>>(
                    await result.Content.ReadAsStringAsync());
                // Remove the "(View Only)" label from old terms
                foreach (var term in terms)
                {
                    term.Name = term.Name.Replace(VIEW_ONLY_TAG, "").Trim();
                }
                return terms;
            }
            logger.LogError(
                "Received HTTP '{}' on GetTermListAsync: {}",
                result.StatusCode, await result.Content.ReadAsStringAsync());
            throw new ApplicationException(
                $"Could not fetch term list: HTTP {result.StatusCode}: " +
                await result.Content.ReadAsStringAsync());
        }

        public async Task<ICollection<MyPurdueSubject>> GetSubjectListAsync(string termCode)
        {
            // TODO: If there are ever more than 500 subjects, we'll need to implement code to pull
            // down the rest of them.
            var result = await httpClient.GetAsync(
                $"classSearch/get_subject?searchTerm=&term={termCode}&offset=1&max={MAX_PAGINATION_COUNT}");
            if (result.IsSuccessStatusCode)
            {
                var subjects = JsonSerializer.Deserialize<List<MyPurdueSubject>>(
                    await result.Content.ReadAsStringAsync());
                // For some reason, the subject name has the subject abbreviation prepended to it
                // ex. code: `AGRY` / name: `AGRY-Agronomy`
                // let's strip out the prefix.
                foreach (var subject in subjects)
                {
                    subject.Name = subject.Name.Replace($"{subject.Code}-", "");
                }
                return subjects;
            }
            logger.LogError(
                "Received HTTP '{}' on GetSubjectListAsync: {}",
                result.StatusCode, await result.Content.ReadAsStringAsync());
            throw new ApplicationException(
                $"Could not fetch subject list: HTTP {result.StatusCode}: " +
                await result.Content.ReadAsStringAsync());
        }

        public async Task<ICollection<MyPurdueSection>> GetSectionsAsync(string termCode,
            string subjectCode)
        {
            // Using this API appears to require us to have an active "session".
            await EnsureSession(termCode);
            await ResetSearchAsync();
            int pageOffset = 0;
            List<MyPurdueSection> sections = [];
            while (true)
            {
                var result = await httpClient.GetAsync("searchResults/searchResults" +
                    $"?txt_subject={subjectCode}&txt_term={termCode}&startDatepicker=" +
                    $"&endDatepicker=&uniqueSessionId={myPurdueSession.Value.sessionId}" +
                    $"&pageOffset={pageOffset}&pageMaxSize={MAX_PAGINATION_COUNT}" +
                    "&sortColumn=subjectDescription&sortDirection=asc");
                if (!result.IsSuccessStatusCode)
                {
                    logger.LogError(
                        "Received HTTP '{}' on GetSectionsAsync: {}",
                        result.StatusCode, await result.Content.ReadAsStringAsync());
                    throw new ApplicationException(
                        $"Could not fetch section list: HTTP {result.StatusCode}: " +
                        await result.Content.ReadAsStringAsync());
                }
                var parsed = JsonSerializer.Deserialize<MyPurdueSectionResponse>(
                    await result.Content.ReadAsStringAsync());
                if (!parsed.Success) {
                    logger.LogError(
                        "Received error on GetSectionsAsync: `success` not set to true");
                    throw new ApplicationException(
                        "Received error on GetSectionsAsync: `success` not set to true");
                }
                var wrongSubject = parsed.Sections.FirstOrDefault(
                    s => s.SubjectCode != subjectCode);
                if (wrongSubject != null)
                {
                    throw new ApplicationException(
                        $"Search for {subjectCode} returned a {wrongSubject.SubjectCode} section " +
                        $"(CRN {wrongSubject.Crn}); search state is stale.");
                }
                sections.AddRange(parsed.Sections);
                pageOffset = sections.Count;
                if (sections.Count >= parsed.TotalCount)
                {
                    break;
                }
                if (parsed.Sections.Count == 0)
                {
                    // An incomplete scrape would cause the sync to delete the missing sections.
                    throw new ApplicationException(
                        $"Search for {subjectCode} stopped at {sections.Count} of " +
                        $"{parsed.TotalCount} sections.");
                }
            }
            return sections;
        }

        public async Task<MyPurdueLinkedSectionsResponse> GetLinkedSectionsAsync(
            string termCode, string crn)
        {
            var result = await httpClient.GetAsync(
                $"searchResults/fetchLinkedSections?term={termCode}&courseReferenceNumber={crn}");
            if (result.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<MyPurdueLinkedSectionsResponse>(
                    await result.Content.ReadAsStringAsync());
            }
            logger.LogError(
                "Received HTTP '{}' on GetLinkedSectionsAsync: {}",
                result.StatusCode, await result.Content.ReadAsStringAsync());
            throw new ApplicationException(
                $"Could not fetch linked sections: HTTP {result.StatusCode}: " +
                await result.Content.ReadAsStringAsync());
        }

        public async Task<ICollection<ICollection<MyPurdueSection>>> GroupLinkedSectionsAsync(
            string termCode, ICollection<MyPurdueSection> sections)
        {
            var byCrn = sections.ToDictionary(s => s.Crn);

            var linkGroups = sections
                .Where(s => s.IsSectionLinked)
                .GroupBy(s => (s.SubjectCourseNumber, s.LinkIdentifier))
                .ToDictionary(g => g.Key, g => g.Select(s => s.Crn).ToList());

            var remaining = new HashSet<string>(byCrn.Keys);
            var groups = new List<HashSet<string>>();
            var owner = new Dictionary<string, HashSet<string>>(); // CRN -> group containing it

            // Closed = every link-ID group touched by this group is entirely inside it.
            // Only used to decide when to stop making calls, never to add sections.
            bool IsClosed(HashSet<string> groupCrns) =>
                groupCrns.All(crn =>
                    !byCrn.TryGetValue(crn, out var s) ||
                    !linkGroups.TryGetValue((s.SubjectCourseNumber, s.LinkIdentifier), out var members) ||
                    members.All(groupCrns.Contains));

            // Seed from labs/recitations/PSOs; expanding their lecture is the cheap direction.
            var ordered = sections.OrderBy(s => s.SectionType == "Lecture");

            foreach (var section in ordered)
            {
                if (!remaining.Remove(section.Crn)) continue;

                if (!section.IsSectionLinked)
                {
                    groups.Add([section.Crn]);
                    continue;
                }

                var groupCrns = new HashSet<string> { section.Crn };
                var neighbors = await GetLinkedCrnsAsync(termCode, section.Crn);
                groupCrns.UnionWith(neighbors);

                // Lectures first: querying one usually closes the group in one call.
                var toExpand = neighbors
                    .Where(crn => crn != section.Crn)
                    .OrderBy(crn => byCrn.TryGetValue(crn, out var s)
                        && s.SectionType == "Lecture" ? 0 : 1)
                    .ToList();

                foreach (var crn in toExpand)
                {
                    if (IsClosed(groupCrns)) break;
                    groupCrns.UnionWith(await GetLinkedCrnsAsync(termCode, crn));
                }

                remaining.ExceptWith(groupCrns);

                // If an early stop ever missed something, a later seed will find it and
                // overlap an existing group; merge instead of producing duplicates.
                var overlapping = groupCrns.Where(owner.ContainsKey)
                    .Select(crn => owner[crn]).Distinct().ToList();

                var target = overlapping.FirstOrDefault() ?? [];
                if (overlapping.Count == 0) groups.Add(target);

                foreach (var other in overlapping.Skip(1))
                {
                    target.UnionWith(other);
                    groups.Remove(other);
                }

                target.UnionWith(groupCrns);
                foreach (var crn in target) owner[crn] = target;
            }

            return groups
                .Select(g => (ICollection<MyPurdueSection>)g
                    .Where(byCrn.ContainsKey) // drop CRNs outside this search (crosslists, etc.)
                    .Select(crn => byCrn[crn])
                    .ToList())
                .ToList();
        }

        private async Task<IReadOnlyCollection<string>> GetLinkedCrnsAsync(string termCode, string crn)
        {
            var response = await GetLinkedSectionsAsync(termCode, crn);
            return response.SectionGroups
                .SelectMany(group => group)
                .Select(s => s.Crn)
                .ToHashSet();
        }

        private async Task EnsureSession(string termCode)
        {
            if (myPurdueSession == null || myPurdueSession.Value.sessionTerm != termCode)
            {
                // Assign ourselves a session ID
                var sessionId = Guid.NewGuid().ToString();

                // Register the session ID with the server
                var form = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["term"] = termCode,
                    ["studyPath"] = "",
                    ["studyPathText"] = "",
                    ["startDatepicker"] = "",
                    ["endDatepicker"] = "",
                    ["uniqueSessionId"] = sessionId,
                });
                var response = await httpClient.PostAsync("term/search?mode=search", form);
                response.EnsureSuccessStatusCode();
                myPurdueSession = (sessionId, termCode);
            }
        }

        private async Task ResetSearchAsync()
        {
            // Clears the session's cached search so the next searchResults call re-queries.
            var response = await httpClient.PostAsync("classSearch/resetDataForm",
                new FormUrlEncodedContent([]));
            response.EnsureSuccessStatusCode();
        }
    }

    namespace Models
    {
        public class MyPurdueLinkedSectionsResponse
        {
            [JsonPropertyName("linkedData")]
            public ICollection<ICollection<MyPurdueSection>> SectionGroups { get; set; }
        }

        public class MyPurdueSectionResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }

            [JsonPropertyName("data")]
            public ICollection<MyPurdueSection> Sections { get; set; }
        }
        public class MyPurdueTerm
        {
            [JsonPropertyName("code")]
            public string Id { get; set; }

            [JsonPropertyName("description")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string Name { get; set; }
        }

        public class MyPurdueSubject
        {
            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("description")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string Name { get; set; }
        }
        public class MyPurdueSection {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("term")]
            public string TermCode { get; set; }

            [JsonPropertyName("termDesc")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string TermName { get; set; }

            [JsonPropertyName("courseReferenceNumber")]
            public string Crn { get; set; }

            [JsonPropertyName("courseNumber")]
            public string CourseNumber { get; set; }

            [JsonPropertyName("subject")]
            public string SubjectCode { get; set; }

            [JsonPropertyName("subjectDescription")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string SubjectName { get; set; }

            [JsonPropertyName("campusDescription")]
            public string CampusName { get; set; }

            [JsonPropertyName("scheduleTypeDescription")]
            public string SectionType { get; set; }

            [JsonPropertyName("courseTitle")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string CourseTitle { get; set; }

            [JsonPropertyName("creditHours")]
            public double? CreditHours { get; set; }

            [JsonPropertyName("creditHourLow")]
            public double? CreditHourLow { get; set; }

            [JsonPropertyName("creditHourHigh")]
            public double? CreditHourHigh { get; set; }

            [JsonPropertyName("creditHourIndicator")]
            public string CreditHourIndicator { get; set; } // "OR", "TO", or null

            [JsonPropertyName("maximumEnrollment")]
            public int MaximumEnrollment { get; set; }

            [JsonPropertyName("enrollment")]
            public int Enrollment { get; set; }

            [JsonPropertyName("seatsAvailable")]
            public int SeatsAvailable { get; set; }

            [JsonPropertyName("faculty")]
            public ICollection<MyPurdueFaculty> Faculty { get; set; }

            [JsonPropertyName("meetingsFaculty")]
            public ICollection<MyPurdueMeeting> Meetings { get; set; }

            [JsonPropertyName("isSectionLinked")]
            public bool IsSectionLinked { get; set; }

            [JsonPropertyName("linkIdentifier")]
            public string LinkIdentifier { get; set; }

            [JsonPropertyName("subjectCourse")]
            public string SubjectCourseNumber { get; set; }

            // fields not mapped:
            // crossList, crossListCapacity, crossListCount, crossListAvailable,
            // reservedSeatSummary, sectionAttributes, instructionalMethod,
            // instructionalMethodDescription, openSection
        }

        public class MyPurdueFaculty {
            [JsonPropertyName("bannerId")]
            public string Id { get; set; }

            [JsonPropertyName("courseReferenceNumber")]
            public string Crn { get; set; }

            [JsonPropertyName("displayName")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string Name { get; set; }

            [JsonPropertyName("emailAddress")]
            public string Email { get; set; }

            [JsonPropertyName("primaryIndicator")]
            public bool IsPrimary { get; set; }

            [JsonPropertyName("term")]
            public string TermCode { get; set; }
        }

        public class MyPurdueMeeting {
            [JsonPropertyName("category")]
            public string Category { get; set; }

            [JsonPropertyName("courseReferenceNumber")]
            public string Crn { get; set; }

            [JsonPropertyName("faculty")]
            public ICollection<MyPurdueFaculty> Faculty { get; set; }

            [JsonPropertyName("meetingTime")]
            public MyPurdueMeetingTime MeetingTime { get; set; }

            [JsonPropertyName("term")]
            public string TermCode { get; set; }
        }

        public class MyPurdueMeetingTime {
            [JsonPropertyName("beginTime")]
            public string BeginTime { get; set; }

            [JsonPropertyName("building")]
            public string BuildingCode { get; set; }

            [JsonPropertyName("buildingDescription")]
            [JsonConverter(typeof(HtmlDecodedStringConverter))]
            public string BuildingName { get; set; }

            [JsonPropertyName("campus")]
            public string CampusCode { get; set; }

            [JsonPropertyName("campusDescription")]
            public string CampusName { get; set; }

            [JsonPropertyName("courseReferenceNumber")]
            public string Crn { get; set; }

            [JsonPropertyName("creditHourSession")]
            public double CreditHours { get; set; }

            [JsonPropertyName("endDate")]
            public string EndDate { get; set; }

            [JsonPropertyName("endTime")]
            public string EndTime { get; set; }

            [JsonPropertyName("hoursWeek")]
            public double HoursPerWeek { get; set; }

            [JsonPropertyName("meetingScheduleType")]
            public string ScheduleType { get; set; }

            [JsonPropertyName("meetingType")]
            public string MeetingType { get; set; }

            [JsonPropertyName("meetingTypeDescription")]
            public string MeetingTypeDescription { get; set; }

            [JsonPropertyName("room")]
            public string Room { get; set; }

            [JsonPropertyName("startDate")]
            public string StartDate { get; set; }

            [JsonPropertyName("term")]
            public string TermCode { get; set; }

            [JsonPropertyName("sunday")]
            public bool IsSunday { get; set; }

            [JsonPropertyName("monday")]
            public bool IsMonday { get; set; }

            [JsonPropertyName("tuesday")]
            public bool IsTuesday { get; set; }

            [JsonPropertyName("wednesday")]
            public bool IsWednesday { get; set; }

            [JsonPropertyName("thursday")]
            public bool IsThursday { get; set; }

            [JsonPropertyName("friday")]
            public bool IsFriday { get; set; }

            [JsonPropertyName("saturday")]
            public bool IsSaturday { get; set; }
        }
    }
}