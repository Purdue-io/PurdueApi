using Crn = System.String;
using HtmlAgilityPack;
using PurdueIo.Scraper.Connections;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PurdueIo.Scraper
{
    public class MyPurdueScraper : IScraper
    {
        private readonly IMyPurdueConnection connection;

        public MyPurdueScraper(IMyPurdueConnection connection)
        {
            this.connection = connection;
        }

        public async Task<ICollection<Term>> GetTermsAsync()
        {
            string termListPageContent = await connection.GetTermListPageAsync();
            var terms = new List<Term>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(termListPageContent);
            HtmlNode root = document.DocumentNode;
            HtmlNodeCollection termSelectNodes = 
                root.SelectNodes("//select[@name='p_term']/option");
            foreach (var node in termSelectNodes)
            {
                var id = node.Attributes["VALUE"].Value;
                if (id.Length <= 0)
                {
                    continue;
                }

                // Remove stuff in parenthesis...
                var name = HtmlEntity.DeEntitize(node.InnerText).Trim();
                Regex parenRegex = new Regex(@"\([^)]*\)", RegexOptions.None);
                name = parenRegex.Replace(name, @"").Trim();

                terms.Add(new Term()
                {
                    Id = id,
                    Name = name
                });
            }
            return terms;
        }

        public async Task<ICollection<Subject>> GetSubjectsAsync(string termCode)
        {
            string subjectListPageContent = await connection.GetSubjectListPageAsync(termCode);
            var subjects = new List<Subject>();
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(subjectListPageContent);
            HtmlNode root = document.DocumentNode;
            HtmlNodeCollection termSelectNodes =
                root.SelectNodes("//select[@id='subj_id'][1]/option");
            foreach (var node in termSelectNodes)
            {
                var code = HtmlEntity.DeEntitize(node.Attributes["VALUE"].Value).Trim();
                var name = HtmlEntity.DeEntitize(node.InnerText).Trim();
                name = name.Substring(name.IndexOf("-")+1);
                subjects.Add(new Subject()
                {
                    Code = code,
                    Name = name
                });
            }
            return subjects;
        }

        public async Task<ICollection<Section>> GetSectionsAsync(string termCode,
            string subjectCode)
        {
            // The section information we need from MyPurdue is split between two pages:
            // the "section list" page (bwckschd.p_get_crse_unsec) and the "section details" page
            // (bwskfcls.P_GetCrse_Advanced).
            //
            // This method scrapes both the section list page and the section details page,
            // then merges the relevant information from both into one coherent model.

            Dictionary<Crn, SectionListInfo> sectionList = 
                await FetchSectionListAsync(termCode, subjectCode);

            Dictionary<Crn, SectionDetailsInfo> sectionDetails =
                await FetchSectionDetailsAsync(termCode, subjectCode);

            var mergedSections = new List<Section>();

            foreach (var sectionPair in sectionList)
            {
                Crn sectionCrn = sectionPair.Key;
                SectionListInfo sectionListInfo = sectionPair.Value;
                if (!sectionDetails.ContainsKey(sectionCrn))
                {
                    throw new ApplicationException($"Section list retrieved from MyPurdue " + 
                        $"contained CRN {sectionCrn} that was not found on section details page");
                }
                SectionDetailsInfo sectionDetailsInfo = sectionDetails[sectionCrn];

                // Merge meeting info
                var mergedSectionMeetings = new List<Meeting>();
                for (int i = 0; i < sectionListInfo.Meetings.Count; ++i)
                {
                    var sectionListInfoMeeting = sectionListInfo.Meetings[i];
                    var sectionDetailsInfoMeeting = sectionDetailsInfo.Meetings[i];
                    mergedSectionMeetings.Add(new Meeting()
                    {
                        Type = sectionDetailsInfoMeeting.Type,
                        Instructors = sectionListInfoMeeting.Instructors,
                        StartDate = sectionListInfoMeeting.StartDate,
                        EndDate = sectionListInfoMeeting.EndDate,
                        DaysOfWeek = sectionListInfoMeeting.DaysOfWeek,
                        StartTime = sectionListInfoMeeting.StartTime,
                        EndTime = sectionListInfoMeeting.EndTime,
                        BuildingCode = sectionDetailsInfoMeeting.BuildingCode,
                        BuildingName = sectionListInfoMeeting.BuildingName,
                        RoomNumber = sectionListInfoMeeting.RoomNumber,
                    });
                }

                // Merge section info
                mergedSections.Add(new Section()
                {
                    Crn = sectionCrn,
                    SectionCode = sectionDetailsInfo.SectionCode,
                    Meetings = mergedSectionMeetings.ToArray(),
                    SubjectCode = sectionListInfo.SubjectCode,
                    CourseNumber = sectionListInfo.CourseNumber,
                    Type = sectionDetailsInfo.Type,
                    CourseTitle = sectionListInfo.CourseTitle,
                    Description = sectionListInfo.Description,
                    CreditHours = sectionListInfo.CreditHours,
                    LinkSelf = sectionListInfo.LinkSelf,
                    LinkOther = sectionListInfo.LinkOther,
                    CampusCode = sectionDetailsInfo.CampusCode,
                    CampusName = sectionListInfo.CampusName,
                    Capacity = sectionDetailsInfo.Capacity,
                    Enrolled = sectionDetailsInfo.Enrolled,
                    RemainingSpace = sectionDetailsInfo.RemainingSpace,
                    WaitListCapacity = sectionDetailsInfo.WaitListCapacity,
                    WaitListCount = sectionDetailsInfo.WaitListCount,
                    WaitListSpace = sectionDetailsInfo.WaitListSpace,
                });
            }

            return mergedSections;
        }

        private async Task<Dictionary<Crn, SectionListInfo>> FetchSectionListAsync(string termCode,
            string subjectCode)
        {
            var parsedSections = new Dictionary<Crn, SectionListInfo>();

            string sectionListPageContent =
                await connection.GetSectionListPageAsync(termCode, subjectCode);

            // Check if we didn't return any classes
            // TODO: Might be a significant perf hit - we can probably avoid searching for this
            // string if the page is large enough that there are likely results.
            if (sectionListPageContent.Contains(
                "No classes were found that meet your search criteria"))
            {
                return parsedSections;
            }

            // Parse HTML from the returned page
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(sectionListPageContent);
            HtmlNode docRoot = htmlDocument.DocumentNode;

            // This will return a table of sections.
            // Every *two* rows is a section.
            HtmlNodeCollection termSelectNodes = docRoot.SelectNodes(
                "/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr");

            // Prepare regex to parse title
            var titleRegex = new Regex(@"^(?<title>.*) - (?<crn>\d{5}) - (?<subj>[A-Z]{2,5}) " +
                @"(?<number>\d{5}) - (?<section>\w{3})(&nbsp;&nbsp;Link Id: (?<selflink>\w{0,12})" +
                @"&nbsp;&nbsp;Linked Sections Required\((?<otherlink>\w{0,12})\))?");

            // Loop through section table and parse each section out
            // Each section occupies two rows.
            for (var i = 0; i < termSelectNodes.Count; i += 2)
            {
                var title = termSelectNodes[i].SelectSingleNode("th").InnerText;
                var titleParse = titleRegex.Match(title);
                if (!titleParse.Success)
                {
                    continue;
                }

                // Grab relevant info from title regex
                string parsedTitle = titleParse.Groups["title"].Value;
                string parsedCrn = titleParse.Groups["crn"].Value;
                string parsedSubjectCode = titleParse.Groups["subj"].Value;
                string parsedCourseNumber = titleParse.Groups["number"].Value;
                string parsedLinkSelf = titleParse.Groups["selflink"].Value;
                string parsedLinkOther = titleParse.Groups["otherlink"].Value;

                // Parse additional information from next row
                string parsedCampusName = "";
                double parsedCreditHours = 0;
                HtmlNode info = termSelectNodes[i + 1].SelectSingleNode("td");
                // TODO: Deal with white space...
                string parsedDescription = HtmlEntity.DeEntitize(info.FirstChild.InnerText).Trim();
                HtmlNode additionalInfo = info.SelectSingleNode("span[@class='fieldlabeltext'][4]");
                while (additionalInfo != null)
                {
                    if (additionalInfo.InnerText.Contains("Campus"))
                    {
                        parsedCampusName = HtmlEntity.DeEntitize(additionalInfo.InnerText.Trim());
                    }
                    if (additionalInfo.InnerText.Contains("Credits"))
                    {
                        parsedCreditHours = double.Parse(
                            HtmlEntity.DeEntitize(additionalInfo.InnerText.Trim()).Split(
                                new string[] { " " }, StringSplitOptions.None)[0]);
                    }
                    additionalInfo = additionalInfo.NextSibling;
                }

                // Parse section meetings
                var parsedMeetings = new List<Meeting>();
                var meetingNodes = info.SelectNodes(
                    "table[@class='datadisplaytable'][1]/tr[ not( th ) ]");
                if (meetingNodes != null) // There is a rare case of a section without meetings.
                {
                    foreach (var meetingNode in meetingNodes)
                    {
                        // Parse times
                        var times = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[2]").InnerText);
                        // TODO: don't hard-code time zones
                        var startEndTimes = ParsingUtilities.ParseStartEndTime(times,
                            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
                        DateTimeOffset parsedMeetingStartTime = startEndTimes.Item1;
                        DateTimeOffset parsedMeetingEndTime = startEndTimes.Item2;

                        // Parse days of week
                        var daysOfWeek = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[3]").InnerText);
                        DaysOfWeek parsedMeetingDaysOfWeek = 
                            ParsingUtilities.ParseDaysOfWeek(daysOfWeek);

                        // Parse building / room
                        string parsedMeetingRoomNumber = "";
                        string parsedMeetingBuildingName = "";
                        string parsedMeetingBuildingCode = "";
                        var room = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[4]").InnerText);
                        if (room.Equals("TBA"))
                        {
                            parsedMeetingRoomNumber = "TBA";
                            parsedMeetingBuildingName = "TBA";
                            parsedMeetingBuildingCode = "TBA";
                        }
                        else
                        {
                            var index = room.LastIndexOf(" ");
                            parsedMeetingBuildingName = room.Substring(0, index);
                            parsedMeetingRoomNumber =
                                room.Substring(index + 1,room.Length - index - 1);
                        }

                        // Parse dates
                        var dates = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[5]").InnerText);
                        // TODO: don't hard-code time zones
                        var startEndDates = ParsingUtilities.ParseStartEndDate(dates,
                            TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
                        DateTimeOffset parsedMeetingStartDate = startEndDates.Item1;
                        DateTimeOffset parsedMeetingEndDate = startEndDates.Item2;

                        // Parse type
                        var type = meetingNode.SelectSingleNode("td[6]").InnerText
                            .Replace("&nbsp;", " ");
                        string parsedMeetingType = type;

                        // Parse instructors
                        var parsedMeetingInstructors = new List<(string name, string email)>();
                        var instructorNodes = meetingNode.SelectNodes("td[7]/a");
                        if (instructorNodes != null)
                        {
                            foreach (var instructorNode in instructorNodes)
                            {
                                var email = instructorNode.Attributes["href"].Value
                                    .Replace("mailto:", "");
                                var name = instructorNode.Attributes["target"].Value;
                                parsedMeetingInstructors.Add((name, email));
                            }
                        }

                        var meeting = new Meeting()
                        {
                            Type = parsedMeetingType,
                            Instructors = parsedMeetingInstructors.ToArray(),
                            StartDate = parsedMeetingStartDate,
                            EndDate = parsedMeetingEndDate,
                            DaysOfWeek = parsedMeetingDaysOfWeek,
                            StartTime = parsedMeetingStartTime,
                            EndTime = parsedMeetingEndTime,
                            BuildingCode = parsedMeetingBuildingCode,
                            BuildingName = parsedMeetingBuildingName,
                            RoomNumber = parsedMeetingRoomNumber,
                        };

                        parsedMeetings.Add(meeting);
                    }
                }

                var section = new SectionListInfo()
                {
                    Crn = parsedCrn,
                    Meetings = parsedMeetings.ToArray(),
                    SubjectCode = parsedSubjectCode,
                    CourseNumber = parsedCourseNumber,
                    CourseTitle = parsedTitle,
                    Description = parsedDescription,
                    CreditHours = parsedCreditHours,
                    LinkSelf = parsedLinkSelf,
                    LinkOther = parsedLinkOther,
                    CampusName = parsedCampusName,
                };

                parsedSections.Add(section.Crn, section);
            }

            return parsedSections;
        }

        private async Task<Dictionary<Crn, SectionDetailsInfo>> FetchSectionDetailsAsync(
            string termCode, string subjectCode)
        {
            var parsedSections = new Dictionary<Crn, SectionDetailsInfo>();

            string sectionDetailsPageContent =
                await connection.GetSectionDetailsPageAsync(termCode, subjectCode);

            // Check if we didn't return any classes
            // TODO: Might be a significant perf hit - we can probably avoid searching for this
            // string if the page is large enough that there are likely results.
            if (sectionDetailsPageContent.Contains(
                "No classes were found that meet your search criteria"))
            {
                return parsedSections;
            }

            // Parse HTML from the returned page
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(sectionDetailsPageContent);
            HtmlNode docRoot = htmlDocument.DocumentNode;
            HtmlNodeCollection sectionNodes = docRoot.SelectNodes(
                "/html/body/div[@class='pagebodydiv'][1]//" + 
                "table[@class='datadisplaytable'][1]/tr[ not ( th ) ]");
            if (sectionNodes == null)
            {
                throw new ApplicationException("Could not parse data from section details page.");
            }

            // Loop through table rows
            SectionDetailsInfo section = null;
            for (var i = 0; i < sectionNodes.Count; i++)
            {
                var node = sectionNodes[i];
                var crnNode = node.SelectSingleNode("td[2]");
                if (crnNode == null)
                {
                    continue; // No node? Skip...
                }

                // Each row is a section AND/OR meeting.
                // If there's a CRN in this row, it means that we're looking at a new section.
                if (HtmlEntity.DeEntitize(crnNode.InnerText).Trim().Length > 0)
                {
                    // Section w/ primary meeting data
                    var crnNumber = HtmlEntity.DeEntitize(crnNode.InnerText).Trim();
                    // Deal with credit hours...
                    var credits = HtmlEntity.DeEntitize(node.SelectSingleNode("td[7]").InnerText)
                        .Trim();
                    if (credits.Contains("-")) {
                        credits = credits.Substring(credits.IndexOf("-")+1);
                    }
                    else if (credits.Contains("/"))
                    {
                        credits = credits.Substring(credits.IndexOf("/") + 1);
                    }
                    section = new SectionDetailsInfo()
                    {
                        Crn = crnNumber,
                        SectionCode = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[5]").InnerText).Trim(),
                        Meetings = new List<SectionDetailsMeetingInfo>(),
                        SubjectCode = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[3]").InnerText).Trim(),
                        CourseNumber = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[4]").InnerText).Trim(),
                        Type = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[23]").InnerText).Trim(),
                        CourseTitle = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[8]").InnerText).Trim(),
                        Description = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[26]").InnerText).Trim(),
                        CreditHours = double.Parse(credits),
                        CampusCode = HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[6]").InnerText).Trim(),
                        Capacity = Int32.Parse(HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[11]").InnerText).Trim()),
                        Enrolled = Int32.Parse(HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[12]").InnerText).Trim()),
                        RemainingSpace = Int32.Parse(HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[13]").InnerText).Trim()),
                        WaitListCapacity = Int32.Parse(HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[14]").InnerText).Trim()),
                        WaitListCount = Int32.Parse(HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[15]").InnerText).Trim()),
                        WaitListSpace = Int32.Parse(HtmlEntity.DeEntitize(
                            node.SelectSingleNode("td[16]").InnerText).Trim()),
                    };

                    parsedSections.Add(crnNumber, section);
                }

                // Now, update meeting data for this row
                // Update meeting days of the week
                // Parse days of week
                var daysOfWeek = HtmlEntity.DeEntitize(
                    node.SelectSingleNode("td[9]").InnerText).Trim();
                DaysOfWeek parsedMeetingDaysOfWeek = ParsingUtilities.ParseDaysOfWeek(daysOfWeek);

                // Parse times
                var times = HtmlEntity.DeEntitize(node.SelectSingleNode("td[10]").InnerText).Trim();
                // TODO: Don't hard-code time zone
                var startEndTimes = ParsingUtilities.ParseStartEndTime(times,
                    TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"));
                DateTimeOffset parsedMeetingStartTime = startEndTimes.Item1;
                DateTimeOffset parsedMeetingEndTime = startEndTimes.Item2;

                // Update meeting location (building short name)
                var loc = HtmlEntity.DeEntitize(node.SelectSingleNode("td[22]").InnerText).Trim();
                string parsedMeetingBuildingCode = "";
                string parsedMeetingBuildingName = "";
                string parsedMeetingRoomNumber = "";
                if (loc.Equals("TBA"))
                {
                    parsedMeetingBuildingCode = "TBA";
                    parsedMeetingBuildingName = "TBA";
                    parsedMeetingRoomNumber = "TBA";
                }
                else if (loc.Length > 0)
                {
                    if (loc.Contains(" "))
                    {
                        parsedMeetingBuildingCode = loc.Substring(0, loc.IndexOf(" ")).Trim();
                        parsedMeetingRoomNumber = loc.Substring(loc.IndexOf(" ") + 1).Trim();
                    }
                    else
                    {
                        parsedMeetingBuildingCode = loc;
                        parsedMeetingRoomNumber = "";
                    }
                } else
                {
                    throw new ApplicationException(
                        $"Could not parse location data for section CRN {section.Crn}.");
                }

                // Updating meeting type
                string parsedMeetingType = HtmlEntity.DeEntitize(
                    node.SelectSingleNode("td[23]").InnerText).Trim();

                // Add the meeting
                section.Meetings.Add(new SectionDetailsMeetingInfo()
                {
                    Type = parsedMeetingType,
                    DaysOfWeek = parsedMeetingDaysOfWeek,
                    StartTime = parsedMeetingStartTime,
                    EndTime = parsedMeetingEndTime,
                    BuildingCode = parsedMeetingBuildingCode,
                    BuildingName = parsedMeetingBuildingName,
                    RoomNumber = parsedMeetingRoomNumber,
                });
            }

            return parsedSections;
        }

        // A subset of Section information that can be scraped from the section list page.
        private record SectionListInfo
        {
            public Crn Crn { get; init; }

            public IList<Meeting> Meetings { get; init; }

            public string SubjectCode { get; init; }

            public string CourseNumber { get; init; }

            public string CourseTitle { get; init; }

            public string Description { get; init; }

            public double CreditHours { get; init; }

            public string LinkSelf { get; init; }

            public string LinkOther { get; init; }

            public string CampusName { get; init; }
        }

        // A subset of Section information that can be scraped from the section details page.
        private record SectionDetailsInfo
        {
            public Crn Crn { get; init; }

            public string SectionCode { get; init; }

            public IList<SectionDetailsMeetingInfo> Meetings { get; init; }

            public string SubjectCode { get; init; }

            public string CourseNumber { get; init; }

            public string Type { get; init; }

            public string CourseTitle { get; init; }

            public string Description { get; init; }
            
            public double CreditHours { get; init; }

            public string CampusCode { get; init; }

            public int Capacity { get; init; }

            public int Enrolled { get; init; }

            public int RemainingSpace { get; init; }

            public int WaitListCapacity { get; init; }

            public int WaitListCount { get; init; }

            public int WaitListSpace { get; init; }
        }

        // A subset of Meeting information that can be scraped from the section details page
        private record SectionDetailsMeetingInfo
        {
            public string Type { get; init; }

            public DaysOfWeek DaysOfWeek { get; init; }

            public DateTimeOffset StartTime { get; init; }

            public DateTimeOffset EndTime { get; init; }

            public string BuildingCode { get; init; }

            public string BuildingName { get; init; }

            public string RoomNumber { get; init; }
        }
    }
}