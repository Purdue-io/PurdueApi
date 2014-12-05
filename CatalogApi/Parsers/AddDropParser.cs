using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	public class AddDropParser : IParser<bool>
	{
		/// <summary>
		/// Checks to see if the registration attempt encountered any errors.
		/// Throws an exception with error messages on error.
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public bool ParseHtml(string content)
		{
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(content);
			HtmlNode docRoot = document.DocumentNode;

			HtmlNode errorTableNode = docRoot.SelectSingleNode("/html/body//table[@summary='This layout table is used to present Registration Errors.']");
			if (errorTableNode == null)
			{
				return true;
			}
			var errorRowNodes = errorTableNode.SelectNodes("tr[ not( th ) ]");
			string errorMessage = "";
			foreach (var errorRow in errorRowNodes)
			{
				errorMessage += errorRow.SelectSingleNode("./td[2]").InnerText + ": " + errorRow.SelectSingleNode("./td[1]").InnerText + "\n";
			}

			throw new ApplicationException(errorMessage);
		}
	}
}
