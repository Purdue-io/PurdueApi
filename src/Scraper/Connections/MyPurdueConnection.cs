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
                return JsonSerializer.Deserialize<List<MyPurdueTerm>>(
                    await result.Content.ReadAsStringAsync());
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

                // the subject names returned from the service are a little funky - they have
                // html-encoded characters and are prefixed with their codes.
                foreach (var subject in subjects)
                {
                    subject.Name = WebUtility.HtmlDecode(subject.Name);
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
                        $"Could not fetch subject list: HTTP {result.StatusCode}: " +
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
                sections.AddRange(parsed.Sections);
                pageOffset = parsed.Sections.Count;
                if (sections.Count >= parsed.TotalCount)
                {
                    break;
                }
            }
            return sections;
        }

        public async Task<MyPurdueLinkedSectionsResponse> GetLinkedSectionsAsync(
            string termCode, string crn)
        {
            var result = await httpClient.GetAsync(
                $"searchResults/fetchLinkedSections?term={termCode}&courseReferenceNumber{crn}");
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
            var sectionMap = sections.ToDictionary(s => s.Crn);
            while (sectionMap.Count > 0)
            {
                var sectionGroup = new List<MyPurdueSection>();
                var startingSection = sectionMap.Last().Value;
                sectionMap.Remove(startingSection.Crn);
                sectionGroup.Add(startingSection);
                while (true)
                {
                    var linkedResponse = await GetLinkedSectionsAsync(termCode,
                        startingSection.Crn);
                    var linkedSections = linkedResponse.SectionGroups;
                }
            }
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
            public string Id { get; init; }

            [JsonPropertyName("description")]
            public string Name { get; init; }
        }

        public class MyPurdueSubject
        {
            [JsonPropertyName("code")]
            public string Code { get; set; }

            [JsonPropertyName("description")]
            public string Name { get; set; }
        }
        public class MyPurdueSection {
            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("term")]
            public string TermCode { get; set; }

            [JsonPropertyName("termDesc")]
            public string TermName { get; set; }

            [JsonPropertyName("courseReferenceNumber")]
            public string Crn { get; set; }

            [JsonPropertyName("courseNumber")]
            public string CourseNumber { get; set; }

            [JsonPropertyName("subject")]
            public string SubjectCode { get; set; }

            [JsonPropertyName("subjectDescription")]
            public string SubjectName { get; set; }

            [JsonPropertyName("campusDescription")]
            public string CampusName { get; set; }

            [JsonPropertyName("scheduleTypeDescription")]
            public string SectionType { get; set; }

            [JsonPropertyName("courseTitle")]
            public string CourseTitle { get; set; }

            [JsonPropertyName("creditHours")]
            public double? CreditHours { get; set; }

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

            // fields not mapped:
            // crossList, crossListCapacity, crossListCount, crossListAvailable, creditHourHigh
            // creditHourLow, creditHourIndicator, openSection, linkIdentifier, isSectionLinked,
            // subjectCourse, reservedSeatSummary, sectionAttributes, instructionalMethod,
            // instructionalMethodDescription
        }

        public class MyPurdueFaculty {
            [JsonPropertyName("bannerId")]
            public string Id { get; set; }

            [JsonPropertyName("courseReferenceNumber")]
            public string Crn { get; set; }

            [JsonPropertyName("displayName")]
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