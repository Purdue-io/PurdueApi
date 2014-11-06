using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi.Parsers
{
	/// <summary>
	/// This interface defines the behavior of a parser meant to
	/// take HTML from a myPurdue request and turn it into some sort
	/// of model we can actually use.
	/// </summary>
	/// <typeparam name="T">Object resulting from parsed HTML.</typeparam>
	public interface IParser<T>
	{
		T ParseHtml(string content);
	}
}
