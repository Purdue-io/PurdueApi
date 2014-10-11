using CatalogSync.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CatalogSync
{
	class CatalogApi
	{
		public static string default_username = "";
		public static string default_password = "";

		private string username = "";
		private string password = "";
		private string referrer = "";
		private CookieContainer cookies = new CookieContainer();

		public CatalogApi(string u = null, string p = null)
		{
			if (u == null)
			{
				username = CatalogApi.default_username;
				password = CatalogApi.default_password;
			}
			else
			{
				username = u;
				password = p;
			}
		}

		private async Task<HttpResponseMessage> Request(string url, FormUrlEncodedContent post_content = null, Boolean follow_redirect = true)
		{
			HttpClientHandler handler = new HttpClientHandler()
			{
				CookieContainer = cookies,
				AllowAutoRedirect = false
			};
			HttpClient c = new HttpClient(handler as HttpMessageHandler)
			{
				BaseAddress = new Uri(url)
			};
			if (referrer.Length > 0)
				c.DefaultRequestHeaders.Referrer = new Uri(referrer);
			System.Diagnostics.Debug.WriteLine("Navigating to '" + url + "...");
			HttpResponseMessage result = await c.PostAsync(url, post_content);
			referrer = url;
			
			if (follow_redirect && result.Headers.Location != null)
			{
				System.Diagnostics.Debug.WriteLine("Referrer detected. Pursuing...");
				result = await Request(result.Headers.Location.ToString());
			}
			return result;
		}
		public async Task<bool> HasValidCredentials()
		{
			return await Authenticate();
		}
		private async Task<bool> Authenticate()
		{

			FormUrlEncodedContent content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("pass", password),
				new KeyValuePair<string, string>("user", username),
				new KeyValuePair<string, string>("uuid", "0xACA021")
			});

			await Request("https://wl.mypurdue.purdue.edu/cp/home/loginf");
			await Request("https://wl.mypurdue.purdue.edu/cp/home/displaylogin"); //Should set a ton of cookies ...
			HttpResponseMessage r = await Request("https://wl.mypurdue.purdue.edu/cp/home/login", content);
			string result = await r.Content.ReadAsStringAsync();
			return (result.Contains("Login Successful"));
		}

		public async Task<ICollection<MyPurdueTerm>> FetchTermList()
		{
			await Authenticate();
			referrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched";
			HttpResponseMessage t = await Request("https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
			string tr = await t.Content.ReadAsStringAsync();

			///prod/bwskfcls.p_sel_crse_search
			HttpResponseMessage s = await Request("https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
			string sr = await s.Content.ReadAsStringAsync();
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(sr);
			HtmlNode root = document.DocumentNode;
			HtmlNodeCollection termSelectNodes = root.SelectNodes("//select[@id='term_input_id'][1]/option");
			var terms = new List<MyPurdueTerm>();
			foreach (var node in termSelectNodes)
			{
				terms.Add(new MyPurdueTerm() {
					Id = node.Attributes["VALUE"].Value,
					Name = node.NextSibling.InnerText
				});
			}
			return terms;
		}

		public async Task<Dictionary<string,MyPurdueSection>> FetchSectionList(string termCode, string subjectCode)
		{
			await Authenticate();
			referrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched";
			var request = await Request("https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");

			FormUrlEncodedContent content = new FormUrlEncodedContent(new[] 
            {
				new KeyValuePair<string, string>("p_calling_proc", "bwckschd.p_disp_dyn_sched"),
                new KeyValuePair<string, string>("term_in", termCode)
			});

			HttpResponseMessage r = await Request("https://selfservice.mypurdue.purdue.edu/prod/bwckgens.p_proc_term_date", content);

			// Construct our "query"
			content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("term_in", termCode),
				new KeyValuePair<string, string>("sel_subj", "dummy"),
				new KeyValuePair<string, string>("sel_day", "dummy"),
				new KeyValuePair<string, string>("sel_schd", "dummy"),
				new KeyValuePair<string, string>("sel_insm", "dummy"),
				new KeyValuePair<string, string>("sel_camp", "dummy"),
				new KeyValuePair<string, string>("sel_levl", "dummy"),
				new KeyValuePair<string, string>("sel_sess", "dummy"),
				new KeyValuePair<string, string>("sel_instr", "dummy"),
				new KeyValuePair<string, string>("sel_ptrm", "dummy"),
				new KeyValuePair<string, string>("sel_attr", "dummy"),
				new KeyValuePair<string, string>("sel_subj", subjectCode),
				new KeyValuePair<string, string>("sel_crse", "%"),
				new KeyValuePair<string, string>("sel_title", ""),
				new KeyValuePair<string, string>("sel_schd", "%"),
				new KeyValuePair<string, string>("sel_from_cred", ""),
				new KeyValuePair<string, string>("sel_to_cred", ""),
				new KeyValuePair<string, string>("sel_camp", "%"),
				new KeyValuePair<string, string>("sel_ptrm", "%"),
				new KeyValuePair<string, string>("sel_instr", "%"),
				new KeyValuePair<string, string>("sel_sess", "%"),
				new KeyValuePair<string, string>("sel_attr", "%"),
				new KeyValuePair<string, string>("begin_hh", "0"),
				new KeyValuePair<string, string>("begin_mi", "0"),
				new KeyValuePair<string, string>("begin_ap", "a"),
				new KeyValuePair<string, string>("end_hh", "0"),
				new KeyValuePair<string, string>("end_mi", "0"),
				new KeyValuePair<string, string>("end_ap", "a"),
			});

			HttpResponseMessage rClasses = await Request("https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_get_crse_unsec", content);
			string rClassesContent = await rClasses.Content.ReadAsStringAsync();
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(rClassesContent);
			HtmlNode docRoot = document.DocumentNode;

			// This will return a table of sections.
			// Every *two* rows is a section.
			HtmlNodeCollection termSelectNodes = docRoot.SelectNodes("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr");

			// Prepare regex to parse title
			string strRegex = @"^(?<title>.*) - (?<crn>\d{5}) - (?<subj>[A-Z]{2,5}) (?<number>\d{5}) - (?<section>\w{3})(&nbsp;&nbsp;Link Id: (?<selflink>\w{0,12})&nbsp;&nbsp;Linked Sections Required\((?<otherlink>\w{0,12})\))?";
			var regexTitle = new Regex(strRegex);

			// Prepare section list
			//var sections = new List<MyPurdueSection>();
			var sections = new Dictionary<string, MyPurdueSection>();

			// Loop through each listing and parse it out
			for (var i = 0; i < termSelectNodes.Count; i+=2) // NOTE +=2 HERE
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
				section.Description = info.FirstChild.InnerText.Trim(); // TODO: Deal with white space...

				var meetingNodes = info.SelectNodes("table[@class='datadisplaytable'][1]/tr[ not( th ) ]");
				foreach (var meetingNode in meetingNodes)
				{
					var meeting = new MyPurdueMeeting();

					// Parse times
					var times = meetingNode.SelectSingleNode("td[2]").InnerText.Split(new string[]{ " - " }, StringSplitOptions.None);
					if (times.Count() != 2)
					{
						meeting.StartTime = DateTimeOffset.MinValue;
						meeting.EndTime = DateTimeOffset.MinValue;
					}
					else
					{
						meeting.StartTime = DateTimeOffset.Parse(times[0]);
						meeting.EndTime = DateTimeOffset.Parse(times[1]);
					}

					// Parse days of week
					var daysOfWeek = meetingNode.SelectSingleNode("td[3]").InnerText.Replace("&nbsp;","");
					if (daysOfWeek.Contains("M")) meeting.DaysOfWeek.Add(DayOfWeek.Monday);
					if (daysOfWeek.Contains("T")) meeting.DaysOfWeek.Add(DayOfWeek.Tuesday);
					if (daysOfWeek.Contains("W")) meeting.DaysOfWeek.Add(DayOfWeek.Wednesday);
					if (daysOfWeek.Contains("R")) meeting.DaysOfWeek.Add(DayOfWeek.Thursday);
					if (daysOfWeek.Contains("F")) meeting.DaysOfWeek.Add(DayOfWeek.Friday);
					if (daysOfWeek.Contains("S")) meeting.DaysOfWeek.Add(DayOfWeek.Saturday);
					if (daysOfWeek.Contains("U")) meeting.DaysOfWeek.Add(DayOfWeek.Sunday);


					// Parse building / room
					var room = meetingNode.SelectSingleNode("td[4]").InnerText;
					if (room.Equals("TBA"))
					{
						meeting.RoomNumber = "TBA";
						meeting.BuildingName = "TBA";
					}
					else
					{
						var index = room.LastIndexOf(" ");
						meeting.BuildingName = room.Substring(0, index);
						meeting.RoomNumber = room.Substring(index + 1, room.Length - index - 1);
					}

					// Parse dates
					var dates = meetingNode.SelectSingleNode("td[5]").InnerText.Replace("&nbsp;", " ");
					if (dates.Equals("TBA"))
					{
						meeting.StartDate = DateTime.MinValue;
						meeting.EndDate = DateTime.MinValue;
					}
					else
					{
						var dateArray = dates.Split(new string[] { " - " }, StringSplitOptions.None);
						meeting.StartDate = DateTime.Parse(dateArray[0]);
						meeting.EndDate = DateTime.Parse(dateArray[1]);
					}

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

				sections.Add(section.Crn,section);
			}
			sections = await _FetchSectionDetails(termCode, subjectCode, sections);

			return sections;
		}

		private async Task<Dictionary<string,MyPurdueSection>> _FetchSectionDetails(string termCode, string subjectCode, Dictionary<string, MyPurdueSection> sections)
		{
			await Authenticate();

			// Now we have to fetch details (enrollment)
			referrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_LOOKCLASS";
			var loginRequest = await Request("https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_LOOKCLASS");
			var loginResponse = await loginRequest.Content.ReadAsStringAsync();

			// Construct our "query"
			var detailsContent = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("rsts", "dummy"),
				new KeyValuePair<string, string>("crn", "dummy"),
				new KeyValuePair<string, string>("term_in", termCode),
				new KeyValuePair<string, string>("sel_subj", "dummy"),
				new KeyValuePair<string, string>("sel_day", "dummy"),
				new KeyValuePair<string, string>("sel_schd", "dummy"),
				new KeyValuePair<string, string>("sel_insm", "dummy"),
				new KeyValuePair<string, string>("sel_camp", "dummy"),
				new KeyValuePair<string, string>("sel_levl", "dummy"),
				new KeyValuePair<string, string>("sel_sess", "dummy"),
				new KeyValuePair<string, string>("sel_instr", "dummy"),
				new KeyValuePair<string, string>("sel_ptrm", "dummy"),
				new KeyValuePair<string, string>("sel_attr", "dummy"),
				new KeyValuePair<string, string>("sel_subj", subjectCode),
				new KeyValuePair<string, string>("sel_crse", ""),
				new KeyValuePair<string, string>("sel_title", ""),
				new KeyValuePair<string, string>("sel_schd", "%"),
				new KeyValuePair<string, string>("sel_from_cred", ""),
				new KeyValuePair<string, string>("sel_to_cred", ""),
				new KeyValuePair<string, string>("sel_camp", "%"),
				new KeyValuePair<string, string>("sel_ptrm", "%"),
				new KeyValuePair<string, string>("sel_instr", "%"),
				new KeyValuePair<string, string>("sel_sess", "%"),
				new KeyValuePair<string, string>("sel_attr", "%"),
				new KeyValuePair<string, string>("begin_hh", "0"),
				new KeyValuePair<string, string>("begin_mi", "0"),
				new KeyValuePair<string, string>("begin_ap", "a"),
				new KeyValuePair<string, string>("end_hh", "0"),
				new KeyValuePair<string, string>("end_mi", "0"),
				new KeyValuePair<string, string>("end_ap", "a"),
				new KeyValuePair<string, string>("SUB_BTN", "Section Search"),
				new KeyValuePair<string, string>("path", "1"),
			});

			referrer = "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse";
			HttpResponseMessage rDetails = await Request("https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse_Advanced", detailsContent);
			string rDetailsContent = await rDetails.Content.ReadAsStringAsync();
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(rDetailsContent);
			HtmlNode docRoot = document.DocumentNode;

			HtmlNodeCollection sectionNodes = docRoot.SelectNodes("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr[ not ( th ) ]");
			int meetingIndex = 0;
			MyPurdueSection section = null;
			for (var i=0; i < sectionNodes.Count; i++) {
				var node = sectionNodes[i];
				var crnNode = node.SelectSingleNode("td[2]");
				if (crnNode == null) continue;
				if (HtmlEntity.DeEntitize(crnNode.InnerText).Trim().Length > 0) {
					// Section w/ primary meeting data
					var crnNumber = HtmlEntity.DeEntitize(crnNode.InnerText).Trim();
					meetingIndex = 0;
					section = sections[crnNumber];
					// Update attendance data for this section
					section.Capacity = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[11]").InnerText).Trim());
					section.Enrolled = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[12]").InnerText).Trim());
					section.RemainingSpace = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[13]").InnerText).Trim());
					section.WaitlistCapacity = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[14]").InnerText).Trim());
					section.WaitlistCount = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[15]").InnerText).Trim());
					section.WaitlistSpace = Int32.Parse(HtmlEntity.DeEntitize(node.SelectSingleNode("td[16]").InnerText).Trim());
					// Update type
					section.Type = HtmlEntity.DeEntitize(node.SelectSingleNode("td[23]").InnerText).Trim();
				} 
				// Update meeting data

				// Update meeting location (building short name)
				var loc = HtmlEntity.DeEntitize(node.SelectSingleNode("td[22]").InnerText).Trim();
				if (loc != "TBA" && loc.Length > 0)
				{
					section.Meetings[meetingIndex].BuildingCode = loc.Substring(0, loc.IndexOf(" "));
				}

				// Updating meeting type
				section.Meetings[meetingIndex].Type = HtmlEntity.DeEntitize(node.SelectSingleNode("td[23]").InnerText).Trim();

				meetingIndex++;
			}

			return sections;
		}
	}
}
