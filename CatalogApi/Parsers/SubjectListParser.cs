using CatalogApi.Models;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	public class SubjectListParser : IParser<List<MyPurdueSubject>>
	{
		public List<MyPurdueSubject> ParseHtml(string content)
		{
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode root = document.DocumentNode;
			HtmlNodeCollection termSelectNodes = root.SelectNodes("//select[@id='subj_id'][1]/option");
			var subjects = new List<MyPurdueSubject>();
			foreach (var node in termSelectNodes)
			{
				var code = HtmlEntity.DeEntitize(node.Attributes["VALUE"].Value).Trim();
				var name = HtmlEntity.DeEntitize(node.InnerText).Trim();
				name = name.Substring(name.IndexOf("-")+1);
				subjects.Add(new MyPurdueSubject()
				{
					SubjectCode = code,
					SubjectName = name
				});
			}
			return subjects;
		}
	}
}
