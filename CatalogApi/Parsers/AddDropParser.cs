using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	public class AddDropParser : IParser<bool>
	{
		public bool ParseHtml(string content)
		{
			return true;
		}
	}
}
