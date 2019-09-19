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
	public class TermListParser : IParser<List<MyPurdueTerm>>
	{
		public List<MyPurdueTerm> ParseHtml(string content)
		{
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode root = document.DocumentNode;
			HtmlNodeCollection termSelectNodes = root.SelectNodes("//select[@name='p_term']/option");
			var terms = new List<MyPurdueTerm>();
			foreach (var node in termSelectNodes)
			{
				var id = node.Attributes["VALUE"].Value;
				if (id.Length <= 0) continue;

				// Remove stuff in parenthesis...
				var name = HtmlEntity.DeEntitize(node.InnerText).Trim();
				Regex parenRegex = new Regex(@"\([^)]*\)", RegexOptions.None);
				name = parenRegex.Replace(name, @"").Trim();

				terms.Add(new MyPurdueTerm()
				{
					Id = id,
					Name = name
				});
			}
			return terms;
		}
	}
}
