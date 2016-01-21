using CatalogApi.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	/// <summary>
	/// This parser class exists to grab extra details about sections
	/// from myPurdue's "class search".
	/// </summary>
	public class SectionDetailsParser : IParser<Dictionary<string,MyPurdueSection>>
	{
		public Dictionary<string, MyPurdueSection> ParseHtml(string content)
		{
            // Prepare section list
            var sections = new Dictionary<string, MyPurdueSection>();
            MyPurdueSection section = null;

            // Check if we didn't return any classes
            if (content.Contains("No classes were found that meet your search criteria"))
            {
                return sections;
            }

            HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode docRoot = document.DocumentNode;

			HtmlNodeCollection sectionNodes = docRoot.SelectNodes("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr[ not ( th ) ]");
			if (sectionNodes == null)
			{
				throw new ApplicationException("Could not parse data from section details request.");
			}

            // Loop through table rows
            for (var i = 0; i < sectionNodes.Count; i++)
			{
				var node = sectionNodes[i];
				var crnNode = node.SelectSingleNode("td[2]");
				if (crnNode == null) continue; // No node? Skip...

				// Each row is a section AND/OR meeting.
				// If there's a CRN in this row, it means that we're looking at a new section.
				if (HtmlEntity.DeEntitize(crnNode.InnerText).Trim().Length > 0)
				{
					// Section w/ primary meeting data
					var crnNumber = HtmlEntity.DeEntitize(crnNode.InnerText).Trim();
					section = new MyPurdueSection()
					{
						Crn = crnNumber,
						SubjectCode = HtmlEntity.DeEntitize(node.SelectSingleNode("td[3]").InnerText).Trim(),
						Number = HtmlEntity.DeEntitize(node.SelectSingleNode("td[4]").InnerText).Trim(),
						SectionCode = HtmlEntity.DeEntitize(node.SelectSingleNode("td[5]").InnerText).Trim(),
						CampusCode = HtmlEntity.DeEntitize(node.SelectSingleNode("td[6]").InnerText).Trim(),
						Title = HtmlEntity.DeEntitize(node.SelectSingleNode("td[8]").InnerText).Trim(),
						Capacity = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[11]").InnerText).Trim()),
						Enrolled = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[12]").InnerText).Trim()),
						RemainingSpace = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[13]").InnerText).Trim()),
						WaitlistCapacity = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[14]").InnerText).Trim()),
						WaitlistCount = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[15]").InnerText).Trim()),
						WaitlistSpace = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[16]").InnerText).Trim()),
						Type = HtmlEntity.DeEntitize(node.SelectSingleNode("td[23]").InnerText).Trim(),
						Description = HtmlEntity.DeEntitize(node.SelectSingleNode("td[26]").InnerText).Trim(),
						Meetings = new List<MyPurdueMeeting>()
					};

					// Deal with credit hours...
					var credits = HtmlEntity.DeEntitize(node.SelectSingleNode("td[7]").InnerText).Trim();
					if (credits.Contains("-")) {
						credits = credits.Substring(credits.IndexOf("-")+1);
					}
					else if (credits.Contains("/"))
					{
						credits = credits.Substring(credits.IndexOf("/") + 1);
					}
					section.CreditHours = double.Parse(credits);

					sections.Add(crnNumber, section);
				}

				// Now, update meeting data for this row
				var meeting = new MyPurdueMeeting();

				// Update meeting days of the week
				// Parse days of week
				var daysOfWeek = HtmlEntity.DeEntitize(node.SelectSingleNode("td[9]").InnerText).Trim();
				meeting.DaysOfWeek = ParseUtility.ParseDaysOfWeek(daysOfWeek);

				// Parse times
				var times = HtmlEntity.DeEntitize(node.SelectSingleNode("td[10]").InnerText).Trim();
				var startEndTimes = ParseUtility.ParseStartEndTime(times, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")); // TODO: Not hard-code time zone
				meeting.StartTime = startEndTimes.Item1;
				meeting.EndTime = startEndTimes.Item2;

				// Parse dates (removed - no year present, not reliable)
				//var dates = HtmlEntity.DeEntitize(node.SelectSingleNode("td[21]").InnerText);
				//var startEndDates = ParseUtility.ParseStartEndDate(dates, TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")); // TODO: Not hard-code time zone
				//meeting.StartDate = startEndDates.Item1;
				//meeting.EndDate = startEndDates.Item2;

				// Update meeting location (building short name)
				var loc = HtmlEntity.DeEntitize(node.SelectSingleNode("td[22]").InnerText).Trim();
				if (loc.Equals("TBA"))
				{
					meeting.BuildingCode = "TBA";
					meeting.BuildingName = "TBA";
					meeting.RoomNumber = "TBA";
				}
				else if (loc.Length > 0)
				{
                    if (loc.Contains(" "))
                    {
                        meeting.BuildingCode = loc.Substring(0, loc.IndexOf(" ")).Trim();
                        meeting.RoomNumber = loc.Substring(loc.IndexOf(" ") + 1).Trim();
                    } else
                    {
                        meeting.BuildingCode = loc;
                        meeting.RoomNumber = "";
                    }
				} else
                {
                    throw new ApplicationException("Could not parse location data for section CRN " + section.Crn + ".");
                }

				// Updating meeting type
				meeting.Type = HtmlEntity.DeEntitize(node.SelectSingleNode("td[23]").InnerText).Trim();

				// Add the meeting
				section.Meetings.Add(meeting);
			}

			return sections;
		}
	}
}
