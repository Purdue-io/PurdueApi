using CatalogSync.Models;
using HtmlAgilityPack;
using PurdueIoDb.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CatalogSync.Parsers
{
	/// <summary>
	/// This class is meant to parse the 'schedule of classes' provided by myPurdue.
	/// Some details are missing - these are filled in later by results from
	/// SectionDetailsParser.
	/// </summary>
	public class SectionListParser : IParser<Dictionary<string,MyPurdueSection>>
	{
		public Dictionary<string,MyPurdueSection> ParseHtml(string content)
		{
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode docRoot = document.DocumentNode;

			// This will return a table of sections.
			// Every *two* rows is a section.
			HtmlNodeCollection termSelectNodes = docRoot.SelectNodes("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr");

			// Prepare regex to parse title
			string strRegex = @"^(?<title>.*) - (?<crn>\d{5}) - (?<subj>[A-Z]{2,5}) (?<number>\d{5}) - (?<section>\w{3})(&nbsp;&nbsp;Link Id: (?<selflink>\w{0,12})&nbsp;&nbsp;Linked Sections Required\((?<otherlink>\w{0,12})\))?";
			var regexTitle = new Regex(strRegex);

			// Prepare section list
			var sections = new Dictionary<string, MyPurdueSection>();

			// Loop through each listing and parse it out
			for (var i = 0; i < termSelectNodes.Count; i += 2) // NOTE +=2 HERE
			{
				var title = termSelectNodes[i].SelectSingleNode("th").InnerText;
				var titleParse = regexTitle.Match(title);
				if (!titleParse.Success)
				{
					continue;
				}

				// Create new section object
				var section = new MyPurdueSection();

				// Grab relevant info from title regex
				section.Title = titleParse.Groups["title"].Value;
				section.Crn = titleParse.Groups["crn"].Value;
				section.SubjectCode = titleParse.Groups["subj"].Value;
				section.Number = titleParse.Groups["number"].Value;
				section.LinkSelf = titleParse.Groups["selflink"].Value;
				section.LinkOther = titleParse.Groups["otherlink"].Value;

				var info = termSelectNodes[i + 1].SelectSingleNode("td");
				section.Description = HtmlEntity.DeEntitize(info.FirstChild.InnerText).Trim(); // TODO: Deal with white space...

				var additionalInfo = info.SelectSingleNode("span[@class='fieldlabeltext'][4]");
				while (additionalInfo != null)
				{
					if (additionalInfo.InnerText.Contains("Campus"))
					{
						section.CampusName = HtmlEntity.DeEntitize(additionalInfo.InnerText.Trim());
					}
					if (additionalInfo.InnerText.Contains("Credits"))
					{
						section.CreditHours = double.Parse(HtmlEntity.DeEntitize(additionalInfo.InnerText.Trim()).Split(new string[] { " " }, StringSplitOptions.None)[0]);
					}
					additionalInfo = additionalInfo.NextSibling;
				}

				var meetingNodes = info.SelectNodes("table[@class='datadisplaytable'][1]/tr[ not( th ) ]");
				foreach (var meetingNode in meetingNodes)
				{
					var meeting = new MyPurdueMeeting();

					// Parse times
					var times = HtmlEntity.DeEntitize(meetingNode.SelectSingleNode("td[2]").InnerText);
					var startEndTimes = ParseUtility.ParseStartEndTime(times);
					meeting.StartTime = startEndTimes.Item1;
					meeting.EndTime = startEndTimes.Item2;

					// Parse days of week
					var daysOfWeek = HtmlEntity.DeEntitize(meetingNode.SelectSingleNode("td[3]").InnerText);
					meeting.DaysOfWeek = ParseUtility.ParseDaysOfWeek(daysOfWeek);

					// Parse building / room
					var room = HtmlEntity.DeEntitize(meetingNode.SelectSingleNode("td[4]").InnerText);
					if (room.Equals("TBA"))
					{
						meeting.RoomNumber = "TBA";
						meeting.BuildingName = "TBA";
						meeting.BuildingCode = "TBA";
					}
					else
					{
						var index = room.LastIndexOf(" ");
						meeting.BuildingName = room.Substring(0, index);
						meeting.RoomNumber = room.Substring(index + 1, room.Length - index - 1);
					}

					// Parse dates
					var dates = HtmlEntity.DeEntitize(meetingNode.SelectSingleNode("td[5]").InnerText);
					var startEndDates = ParseUtility.ParseStartEndDate(dates);
					meeting.StartDate = startEndDates.Item1;
					meeting.EndDate = startEndDates.Item2;

					// Parse type
					var type = meetingNode.SelectSingleNode("td[6]").InnerText.Replace("&nbsp;", " ");
					meeting.Type = type;

					// Parse instructors
					var instructorNodes = meetingNode.SelectNodes("td[7]/a");
					if (instructorNodes != null)
					{
						foreach (var instructorNode in instructorNodes)
						{
							var email = instructorNode.Attributes["href"].Value.Replace("mailto:", "");
							var name = instructorNode.Attributes["target"].Value;
							meeting.Instructors.Add(new Tuple<string, string>(name, email));
						}
					}

					section.Meetings.Add(meeting);
				}

				sections.Add(section.Crn, section);
			}
			//sections = await _FetchSectionDetails(termCode, subjectCode, sections);

			return sections;
		}
	}
}
