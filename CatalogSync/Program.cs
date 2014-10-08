using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatalogSync
{
	// To learn more about Microsoft Azure WebJobs, please see http://go.microsoft.com/fwlink/?LinkID=401557
	class Program
	{
		static int Main()
		{
			var catalogApi = new CatalogApi("", "");
			var login = catalogApi.HasValidCredentials().Result;
			if (!login)
			{
				System.Console.WriteLine("Couldn't authenticate! Terminating ...");
				return -1;
			}
			catalogApi.FetchTermList().Wait();
			Console.ReadKey();
			return 0;
		}
	}
}
