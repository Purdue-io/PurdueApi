using CatalogApi.Models;
using HtmlAgilityPack;
using PurdueIoDb.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	/// <summary>
	/// This class is meant to parse the 'schedule of classes' provided by myPurdue.
	/// Some details are missing - these are filled in later by results from
	/// SectionDetailsParser.
	/// </summary>
	public class UserScheduleParser : IParser<Dictionary<string,List<string>>>
	{
		public Dictionary<string,List<string>> ParseHtml(string content)
		{
            // Prepare term/crn list
            var registrations = new Dictionary<string, List<string>>();

            // If we are not registered for anything, return nothing.
            if (content.ToLower().Contains("you are not currently registered"))
            {
                return registrations;
            }

            // Load HTML document for parsing
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode docRoot = document.DocumentNode;

			// Here's the list of registered sections ... each section has 11 rows
			HtmlNodeCollection termSelectNodes = docRoot.SelectNodes("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]//tr");
            if (termSelectNodes != null)
            {
                for (var i = 0; i < termSelectNodes.Count; i += 10)
                {
                    var termName = termSelectNodes[i + 1].SelectSingleNode("td[1]").InnerText;
                    var crn = termSelectNodes[i + 2].SelectSingleNode("td[1]/a").InnerText;
                    if (!registrations.ContainsKey(termName))
                    {
                        registrations.Add(termName, new List<string>());
                    }
                    registrations[termName].Add(crn);
                }
            }

			return registrations;
		}
	}
}
