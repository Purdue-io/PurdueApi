using CatalogApi.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	/// <summary>
	/// This parser class exists to grab extra details about sections
	/// from myPurdue's "class search".
	/// </summary>
	public class SectionSeatsParser : IParser<MyPurdueSectionSeats>
	{
		public MyPurdueSectionSeats ParseHtml(string content)
		{
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode docRoot = document.DocumentNode;

			HtmlNode titleNode = docRoot.SelectSingleNode("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr[1]/th[1]");
			if (titleNode == null)
			{
				throw new ApplicationException("Could not parse data from section details request.");
			}

			// Prepare regex to parse title
			string strRegex = @"^(?<title>.*) - (?<crn>\d{5}) - (?<subj>[A-Z]{2,5}) (?<number>\d{5}) - (?<section>\w{3})(&nbsp;&nbsp;Link Id: (?<selflink>\w{0,12})&nbsp;&nbsp;Linked Sections Required\((?<otherlink>\w{0,12})\))?";
			var regexTitle = new Regex(strRegex);

			var title = HtmlEntity.DeEntitize(titleNode.InnerText).Trim();
			var titleParse = regexTitle.Match(title);

			HtmlNode seatsTableNode = docRoot.SelectSingleNode("/html/body/div[@class='pagebodydiv'][1]/table[@class='datadisplaytable'][1]/tr[2]/td[1]/table[1]");
			var seatNodes = seatsTableNode.SelectNodes(".//td[@class='dddefault']");

			var capacity = int.Parse(HtmlEntity.DeEntitize(seatNodes[0].InnerText).Trim());
			var actual = int.Parse(HtmlEntity.DeEntitize(seatNodes[1].InnerText).Trim());
			var remaining = int.Parse(HtmlEntity.DeEntitize(seatNodes[2].InnerText).Trim());
			var waitCapacity = int.Parse(HtmlEntity.DeEntitize(seatNodes[3].InnerText).Trim());
			var waitActual = int.Parse(HtmlEntity.DeEntitize(seatNodes[4].InnerText).Trim());
			var waitRemaining = int.Parse(HtmlEntity.DeEntitize(seatNodes[5].InnerText).Trim());

			var section = new MyPurdueSectionSeats()
			{
				Title = titleParse.Groups["title"].Value,
				Crn = titleParse.Groups["crn"].Value,
				SubjectCode = titleParse.Groups["subj"].Value,
				Number = titleParse.Groups["number"].Value,
				SectionCode = titleParse.Groups["section"].Value,
				Capacity = capacity,
				Enrolled = actual,
				RemainingSpace = remaining,
				WaitlistCapacity = waitCapacity,
				WaitlistCount = waitActual,
				WaitlistSpace = waitRemaining
			};

			return section;
		}
	}
}
