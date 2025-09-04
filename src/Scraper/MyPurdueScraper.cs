using Crn = System.String;
using HtmlAgilityPack;
using PurdueIo.Scraper.Connections;
using PurdueIo.Scraper.Models;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace PurdueIo.Scraper
{
    public class MyPurdueScraper : IScraper
    {
        private readonly IMyPurdueConnection connection;

        private readonly ILogger<MyPurdueScraper> logger;

        public MyPurdueScraper(IMyPurdueConnection connection, ILogger<MyPurdueScraper> logger)
        {
            this.connection = connection;
            this.logger = logger;
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
            HtmlDocument document = new();
            document.LoadHtml(subjectListPageContent);
            HtmlNode root = document.DocumentNode;
            HtmlNodeCollection termSelectNodes =
                root.SelectNodes("//select[@id='subj_id'][1]/option");
            foreach (var node in termSelectNodes)
            {
                var code = HtmlEntity.DeEntitize(node.Attributes["VALUE"].Value).Trim();
                var name = HtmlEntity.DeEntitize(node.InnerText).Trim();
                name = name.Substring(name.IndexOf("-") + 1);
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
            // The section information we need from MyPurdue may be split across
            // multiple pages.
            //
            // This method scrapes the relevant information from all sources into
            // one coherent model.

            Dictionary<Crn, SectionListInfo> sectionList = 
                await FetchSectionListAsync(termCode, subjectCode);

            var mergedSections = new List<Section>();

            foreach (var sectionPair in sectionList)
            {
                Crn sectionCrn = sectionPair.Key;
                SectionListInfo sectionListInfo = sectionPair.Value;

                // Merge meeting info
                var mergedSectionMeetings = new List<Meeting>();
                for (int i = 0; i < sectionListInfo.Meetings.Count; ++i)
                {
                    var sectionListInfoMeeting = sectionListInfo.Meetings[i];
                    if (!BuildingNamesToShortCodes.TryGetValue(sectionListInfoMeeting.BuildingName,
                        out string buildingShortCode))
                    {
                        throw new ApplicationException(
                            "No building short code found for building " +
                            $"'{sectionListInfoMeeting.BuildingName}'");
                    }
                    mergedSectionMeetings.Add(new Meeting()
                    {
                        Type = sectionListInfoMeeting.Type,
                        Instructors = sectionListInfoMeeting.Instructors,
                        StartDate = sectionListInfoMeeting.StartDate,
                        EndDate = sectionListInfoMeeting.EndDate,
                        DaysOfWeek = sectionListInfoMeeting.DaysOfWeek,
                        StartTime = sectionListInfoMeeting.StartTime,
                        EndTime = sectionListInfoMeeting.EndTime,
                        BuildingCode = buildingShortCode,
                        BuildingName = sectionListInfoMeeting.BuildingName,
                        RoomNumber = sectionListInfoMeeting.RoomNumber,
                    });
                }

                string sectionType = sectionListInfo.Meetings.Select(m => m.Type)
                    .FirstOrDefault("");

                if (!CampusNamesToShortCodes.TryGetValue(sectionListInfo.CampusName,
                        out string campusShortCode))
                {
                    throw new ApplicationException(
                        "No campus short code found for campus " +
                        $"'{sectionListInfo.CampusName}'");
                }

                // Merge section info
                mergedSections.Add(new Section()
                {
                    Crn = sectionCrn,
                    SectionCode = sectionListInfo.SectionCode,
                    Meetings = mergedSectionMeetings.ToArray(),
                    SubjectCode = sectionListInfo.SubjectCode,
                    CourseNumber = sectionListInfo.CourseNumber,
                    Type = sectionType,
                    CourseTitle = sectionListInfo.CourseTitle,
                    Description = sectionListInfo.Description,
                    CreditHours = sectionListInfo.CreditHours,
                    LinkSelf = sectionListInfo.LinkSelf,
                    LinkOther = sectionListInfo.LinkOther,
                    CampusCode = campusShortCode,
                    CampusName = sectionListInfo.CampusName,
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
            HtmlDocument htmlDocument = new();
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
                string parsedSectionCode = titleParse.Groups["section"].Value;
                string parsedLinkSelf = titleParse.Groups["selflink"].Value;
                string parsedLinkOther = titleParse.Groups["otherlink"].Value;

                // Parse additional information from next row
                string parsedCampusName = "";
                double parsedCreditHours = 0;
                HtmlNode info = termSelectNodes[i + 1].SelectSingleNode("td");
                // TODO: Deal with white space...
                string parsedDescription = "";
                if (info.FirstChild.NodeType == HtmlNodeType.Text)
                {
                    parsedDescription = HtmlEntity.DeEntitize(info.FirstChild.InnerText).Trim();
                }
                HtmlNode additionalInfo = info
                    .SelectSingleNode("span[@class='fieldlabeltext'][last()]")
                    ?.NextSibling?.NextSibling;
                while (additionalInfo != null)
                {
                    if (additionalInfo.NodeType == HtmlNodeType.Text)
                    {
                        if (additionalInfo.InnerText.Contains("Campus"))
                        {
                            parsedCampusName = HtmlEntity.DeEntitize(
                                additionalInfo.InnerText.Trim());
                        }
                        if (additionalInfo.InnerText.Contains("Credits"))
                        {
                            parsedCreditHours = double.Parse(
                                HtmlEntity.DeEntitize(additionalInfo.InnerText.Trim()).Split(
                                    new string[] { " " }, StringSplitOptions.None)[0]);
                        }
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
                        var startEndTimes = ParsingUtilities.ParseStartEndTime(times);
                        TimeOnly? parsedMeetingStartTime = startEndTimes.Item1;
                        TimeOnly? parsedMeetingEndTime = startEndTimes.Item2;

                        // Parse days of week
                        var daysOfWeek = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[3]").InnerText);
                        DaysOfWeek parsedMeetingDaysOfWeek = 
                            ParsingUtilities.ParseDaysOfWeek(daysOfWeek);

                        // Parse building / room
                        var locationString = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[4]").InnerText);
                        var locationDetails = ParseLocationDetails(locationString);
                        if (locationDetails == null)
                        {
                            throw new ArgumentOutOfRangeException("Could not parse building " +
                                $"and room information from string '{locationString}'");
                        }

                        // Parse dates
                        var dates = HtmlEntity.DeEntitize(
                            meetingNode.SelectSingleNode("td[5]").InnerText);
                        var startEndDates = ParsingUtilities.ParseStartEndDate(dates);
                        DateOnly? parsedMeetingStartDate = startEndDates.Item1;
                        DateOnly? parsedMeetingEndDate = startEndDates.Item2;

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
                            BuildingCode = locationDetails?.buildingShortCode,
                            BuildingName = locationDetails?.buildingName,
                            RoomNumber = locationDetails?.room,
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
                    SectionCode = parsedSectionCode,
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

        // A subset of Section information that can be scraped from the section list page.
        private record SectionListInfo
        {
            public Crn Crn { get; init; }

            public IList<Meeting> Meetings { get; init; }

            public string SubjectCode { get; init; }

            public string CourseNumber { get; init; }

            public string CourseTitle { get; init; }

            public string SectionCode { get; init; }

            public string Description { get; init; }

            public double CreditHours { get; init; }

            public string LinkSelf { get; init; }

            public string LinkOther { get; init; }

            public string CampusName { get; init; }
        }

        // The loss of authenticated APIs removed our source of information for 
        // campus short codes, so now they are hard-coded here and we just hope
        // new campuses are a fairly rare occurrence.
        // Tracked here: https://github.com/Purdue-io/PurdueApi/issues/55
        private static readonly Dictionary<string, string> CampusNamesToShortCodes = new()
        {
            // Updated 2024-09-02 to match the list reported by
            // https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched
            { "Indianapolis and W Lafayette Campus", "PIN" },
            { "SW Anderson Campus", "TAN" },
            { "SW Columbus Campus", "TCO" },
            { "SW Indianapolis Intl Airport Campus", "TDY" },
            { "SW Kokomo Campus", "TKO" },
            { "SW New Albany Campus", "TNA" },
            { "SW Richmond Campus", "TRI" },
            { "SW South Bend Campus", "TSB" },
            { "SW Subaru Manufacturing Campus Campus", "TLF" },
            { "SW Vincennes Campus", "TVN" },
            { "West Lafayette Campus", "PWL" },
            { "West Lafayette Continuing Ed Campus", "CEC" },
            // These entries do not appear on the course schedule search page,
            // but are still found in section listings
            { "Dual Campus Campus", "TDC" },
            { "Concurrent Credit Campus", "CC" },
            { "Indiana Univ Indianapolis Campus", "IUI" },
        };

        // The loss of authenticated APIs removed our source of information for 
        // building short codes, so now they are hard-coded here and we just hope
        // new buildings are a fairly rare occurrence.
        // Tracked here: https://github.com/Purdue-io/PurdueApi/issues/54
        private static readonly Dictionary<string, string> BuildingNamesToShortCodes = new()
        {
            { "Asynchronous Online Learning", "ASYNC" },
            { "TBA", "TBA" },
            { "Chaffee Hall", "CHAF" },
            { "Seng-Liang Wang Hall", "WANG" },
            { "Lawson Computer Science Bldg", "LWSN" },
            { "Synchronous Online Learning", "SYNC" },
            { "Forney Hall of Chemical Engr", "FRNY" },
            { "Grissom Hall", "GRIS" },
            { "Knoy Hall of Technology", "KNOY" },
            { "Mathematical Sciences Building", "MATH" },
            { "Electrical Engineering Bldg", "EE" },
            { "Stewart Center", "STEW" },
            { "Materials and Electrical Engr", "MSEE" },
            { "Wilmeth Active Learning Center", "WALC" },
            { "Stanley Coulter Hall", "SC" },
            { "Honors College&Resid North", "HCRN" },
            { "Physics Building", "PHYS" },
            { "Brown Laboratory of Chemistry", "BRWN" },
            { "Wetherill Lab of Chemistry", "WTHR" },
            { "Hampton Hall of Civil Engnrng", "HAMP" },
            { "Neil Armstrong Hall of Engr", "ARMS" },
            { "Recitation Building", "REC" },
            { "Beering Hall of Lib Arts & Ed", "BRNG" },
            { "Lilly Hall of Life Sciences", "LILY" },
            { "ADM Agricultural Innovation Ct", "ADM" },
            { "Lyles-Porter Hall", "LYLE" },
            { "Agricultural & Biological Engr", "ABE" },
            { "Hicks Undergraduate Library", "HIKS" },
            { "Forest Products Building", "FPRD" },
            { "Pao Hall of Visual & Perf Arts", "PAO" },
            { "Nelson Hall of Food Science", "NLSN" },
            { "Forestry Building", "FORS" },
            { "Armory", "AR" },
            { "Biochemistry Building", "BCHM" },
            { "Class of 1950 Lecture Hall", "CL50" },
            { "Smith Hall", "SMTH" },
            { "Jerry S Rawls Hall", "RAWL" },
            { "Krannert Building", "KRAN" },
            { "Horticulture Building", "HORT" },
            { "On-site", "OFFCMP" },
            { "Daniel Turfgrass Rsch&Diag Ct", "DANL" },
            { "Heavilon Hall", "HEAV" },
            { "University Hall", "UNIV" },
            { "Land O Lakes Ctr", "LOLC" },
            { "Matthews Hall", "MTHW" },
            { "Creighton Hall of Animal Sci", "CRTN" },
            { "Jischke Hall of Biomedical Eng", "MJIS" },
            { "Peirce Hall", "PRCE" },
            { "Winthrop E. Stone Hall", "STON" },
            { "Holleman-Niswonger Simultr Ctr", "SIML" },
            { "Niswonger Aviation Tech Bldg", "NISW" },
            { "Terminal Building (Hangar 2)", "TERM" },
            { "Indiana Manufcturing Institute", "IMI" },
            { "Aerospace Science Lab-Hangar 3", "AERO" },
            { "Composites Laboratory", "COMP" },
            { "Slayter Ctr of Performing Arts", "SCPA" },
            { "Elliott Hall of Music", "ELLT" },
            { "Chaney-Hale Hall of Science", "CHAS" },
            { "Morgan Ctr for Entrepreneurshp", "MRGN" },
            { "Lynn Hall of Vet Medicine", "LYNN" },
            { "University Church", "UC" },
            { "Robert Heine Pharmacy Building", "RHPH" },
            { "Mechanical Engineering Bldg", "ME" },
            { "Vet Pathobiology Research Bldg", "VPRB" },
            { "Veterinary Pathology Building", "VPTH" },
            { "Psychological Sciences Bldg", "PSYC" },
            { "Felix Haas Hall", "HAAS" },
            { "Marriott Hall", "MRRT" },
            { "Potter Engineering Center", "POTR" },
            { "Lambert Field House & Gym", "LAMB" },
            { "Brees Student-Athlete Acad Ctr", "BRES" },
            { "Krach Leadership Center", "KRCH" },
            { "Ernest C. Young Hall", "YONG" },
            { "Eleanor B Shreve Residence Hal", "SHRV" },
            { "John S. Wright Forestry Center", "WRIT" },
            { "Pfendler Hall of Agriculture", "PFEN" },
            { "Honors College&Resid South", "HCRS" },
            { "Griffin Residence Hall South", "GRFS" },
            { "Fowler Memorial House", "FWLR" },
            { "Bill and Sally Hanley Hall", "HNLY" },
            { "Horticultural Greenhouse", "HGRH" },
            { "State Farm", "SF" },
            { "Johnson Hall of Nursing", "JNSN" },
            { "Schwartz Tennis Center", "SCHW" },
            { "Purdue Memorial Union", "PMU" },
            { "Hillenbrand Residence Hall", "HILL" },
            { "Equine Health Sciences Annex", "EHSA" },
            { "Equine Health Sciences Bldg", "EHSB" },
            { "Online", "ONLINE" },
            { "Hockmeyer Hall Strc Bio", "HOCK" },
            { "The Innovation Center", "INVC" },
            { "Whistler Hall of Ag Research", "WSLR" },
            { "Training & Reception Center ROOM", "TRC" },
            { "Purdue Technology Center", "SEI" },
            { "Technology Statewide Site", "TECHSW" },
            { "Agricultural Administration", "AGAD" },
            { "Drug Discovery Bldg", "DRUG" },
            { "Bechtel Innovation Design Ctr", "BIDC" },
            { "Michael Golden Labs and Shops", "MGL" },
            { "Tom Spurgeon Golf Training Ctr", "SPUR" },
            { "Ground Service Building", "GRS" },
            { "Third Street Suites", "TSS" },
            { "Nuclear Engineering Building", "NUCL" },
            { "Kepner Hall", "KPNR" },
            { "Purdue Memorial Union Club", "PMUC" },
            { "Cordova Rec Sports Center", "CREC" },
            { "Herrick Laboratories", "HLAB" },
            { "Animal Sciences Teaching Lab", "ASTL" },
            { "Cancelled", "CANCEL" },
            { "Ross-Ade Stadium", "STDM" },
            { "Discovery and Learning", "DLR" },
            { "On-site ONLINE", "OFFCMP" },
            { "Black Cultural Center", "BCC" },
            { "Subaru Isuzu Automotive", "SIA" },
            { "Hansen Life Sciences Research", "HANS" },
            { "Animal Disease Diagnostic Lab", "ADDL" },
            { "Guy J Mackey Arena", "MACK" },
            { "Ray W Herrick Laboratory", "HERL" },
            { "Harvey W. Wiley Residence Hall", "WILY" },
            { "Boilermaker Aquatic Center", "AQUA" },
            { "INOK Investments Warehouse", "INOK" },
            { "High Pressure Research Lab", "ZL3" },
            { "Engineering Administration", "ENAD" },
            { "Bindley Bioscience Center", "BIND" },
            { "Krannert Center", "KCTR" },
            { "South Campus Courts Bldg B", "SCCB" },
            { "Civil Engineering Building", "CIVL" },
            { "Recreational Sports Center", "RSC" },
            { "Service Building", "SERV" },
            { "Child Developmt&Family Studies", "CDFS" },
            { "Food Science Building", "FS" },
            { "Birk Nanotechnology Center", "BRK" },
            { "Max W & Maileen Brown Hall", "BHEE" },
            { "Marc & Sharon Hagle Hall", "HAGL" },
            { "Inventrek Technology Park", "INVTRK" },
            { "Advanced Manufacturing Center", "AMCE" },
            { "Learning Center", "LC" },
            { "Purdue Polytechnic Anderson", "PPA" },
            { "Flex Lab", "FLEX" },
            { "Main Street Resource Center", "MSRC" },
            { "Studebaker Building", "SBST" },
            { "Asian Amer & Asian Cult Ctr", "AACC" },
            { "Dudley Hall", "DUDL" },
            { "Gerald D and Edna E Mann Hall", "MANN" },
            { "Lambertus Hall", "LMBS" },
            { "Homeland Security & Public Ser", "HSPS" },
            { "McDaniel Hall", "MHALL" },
            { "Johnson Hall", "JHALL" },
            { "Meredith Residence Hall South", "MRDS" },
            { "OffSite", "OFST" },
            { "Alexandria", "ALEX" },
            { "Helen B. Schleman Hall", "SCHM" },
            { "Winifred Parker Residence Hall", "PKRW" },
            { "Technology Building", "TECHB" },
            { "Aviation Technology Center", "ATC" },
            { "Homeland Security Building", "HSB" },
            { "Muncie Central", "MCHS" }, // I made this short code up since this is a dual-credit
                                          // course at Muncie Central High School, presumably
                                          // doesn't have a real short code.

            // List of IUPUI buildings... this has been a pretty good reference:
            // https://cfs.iupui.edu/about/building-info.html
            // also this:
            // https://cpf.iu.edu/doc/building-list/IU_Bldglist_Public.pdf
            // also this:
            // https://cs.iupui.edu/~jzheng/digitalcity/CampusCompass/facilities.htm
            { "1011 Dr. Martin Luther King, Jr. St.", "MK" },
            { "Ball Annex", "BA" },
            { "Business/SPEA", "BS" },
            { "Biotechnology & Research Training Center", "L3" },
            { "Campus Services 3", "SC" },
            { "Campus Services 4", "BP" },
            { "Cancer Research Institute", "R4" },
            { "Campus Center", "CE" },
            { "Cavanaugh Hall", "CA" },
            { "Center for Young Children", "YC" },
            { "Coleman Hall", "CF" },
            { "Dental School", "DS" },
            { "Dunlap", "DB" },
            { "Education/Social Work", "ES" },
            { "Emerson Hall", "EH" },
            { "Engineering Science & Technology", "SL" },
            { "Eskenazi Fine Arts", "HE" },
            { "Eskenazi Hall", "HR" },
            { "Fesler Hall", "FH" },
            { "Gatch Clinical", "CL" },
            { "Glick Eye Institute", "GK" },
            { "Health Information & Translation Sciences", "HS" },
            { "Health Sciences", "RG" },
            { "Hine Hall", "IP" },
            { "Informatics & Communications Technology Complex", "IT" },
            { "Inlow Hall", "IH" },
            { "IU Innovation Center", "TK" },
            { "Innovation Hall", "IO" },
            { "Kuhn House", "AC" },
            { "Lecture Hall", "LE" },
            { "Lockefield Village", "LV" },
            { "Long Hall", "LO" },
            { "Madam Walker Theatre", "MT" },
            { "Medical Research & Library", "IB" },
            { "Natatorium", "PE" },
            { "Neuroscience Research Building", "NB" },
            { "North Hall", "HM" },
            { "Nursing School", "NU" },
            { "Oral Health", "OH" },
            { "Pediatric Care Center", "PC" },
            { "Research Institute II", "R2" },
            { "Riley Research", "RR" },
            { "Rotary Building", "RO" },
            { "Science Building", "LD" },
            { "Science and Engineering Laboratory Building", "EL" },
            { "Science, Engineering & Technology", "ET" },
            // { "Service Building", "RV" }, // Duplicate key. May not be needed, as it hasn't
                                             // appeared in course listings yet.
            { "Taylor Hall", "UC" },
            // { "University Hall", "AD" }, // Duplicate key. May not be needed, as it hasn't
                                            // appeared in course listings yet.
            { "University Library", "UL" },
            { "VanNuys Medical Science Building", "MS" },
            { "Walther Hall", "R3" },

            // Annoyingly, MyPurdue shortens names of some IUPUI buildings. Those are listed here.
            { "Engineering/Technology", "ET"},
            { "Engr/Science & Tech", "SL" },
            { "ICTC", "ICTC" }, // Not sure why they abbreviated this one...should stand for
                                // "Informatics and Communications Technology Complex" as above
            { "Science & Engineering Lab", "EL" },
            { "Ezkenazi Hall", "HR" }, // Someone at Purdue must have misspelled "Eskenazi"... 🤦

            // Purdue Polytechnic Columbus
            { "Columbus Learning Center", "CLC" },

            // Indiana College Network
            // Course descriptions note:
            //     "Indiana Partnership for Statewide Education distance course."
            // Examples:
            //     Indiana College Network ITCC-BL
            //     Indiana College Network ITCC-GA
            //     Indiana College Network VU
            { "Indiana College Network", "ICN" },

            // New as of term 202520
            { "Hall of Data Science and AI", "DSAI" },

            // New as of term 202610
            // short code found on https://www.campus-maps.com/purdue-university/moll-mollenkopf-athletic-center/
            // seems legit.
            { "Mollenkopf Athletic Center", "MOLL" },
        };

        public (string buildingName, string buildingShortCode, string room)? ParseLocationDetails(
            string locationString)
        {
            // Handle special case
            if ((locationString == "TBA") || locationString.StartsWith("Temp-"))
            {
                return (buildingName: "TBA", buildingShortCode: "TBA", room: "TBA");
            }

            // Building name and room are dilineated by a space
            // (ex. "Lawson Computer Science Bldg B134")
            // Unfortunately, many building names (and some rooms) have spaces in them too.
            // First, let's see if we can find the building in our known list.
            // We do this by walking back each 'token' until we've found one and only one match
            string buildingName = locationString;
            while (true)
            {
                if (BuildingNamesToShortCodes.ContainsKey(buildingName))
                {
                    string shortCode = BuildingNamesToShortCodes[buildingName];
                    string room;
                    if (locationString.Length > buildingName.Length)
                    {
                        room = locationString[(buildingName.Length + 1)..];
                    }
                    else
                    {
                        room = locationString[buildingName.Length..];
                    }
                    return (buildingName: buildingName, buildingShortCode: shortCode, room: room);
                }
                if (!buildingName.Contains(' '))
                {
                    break;
                }
                buildingName = buildingName[..buildingName.LastIndexOf(' ')];
            }

            return null;
        }
    }
}
