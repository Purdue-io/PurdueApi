using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogSync.Parsers
{
	public class TestParser : IParser<bool>
	{
		public bool ParseHtml(string content)
		{
			return (content.Contains("test"));
		}
	}
}
