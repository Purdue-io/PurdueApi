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
		private CatalogApi Api;
		static int Main()
		{
			var p = new Program("", ""); // Credentials go here.
			p.SyncCourses("201510").Wait();
			Console.ReadLine();
			return 0;
		}

		Program(string user, string pass) {
			Api = new CatalogApi(user, pass);
			var catalogApi = new CatalogApi(user, pass);
			var login = catalogApi.HasValidCredentials().Result;
			if (!login) {
				throw new UnauthorizedAccessException("Could not authenticate to myPurdue with the supplied credentials.");
			}
		}

		public async Task SyncCourses(string termId)
		{
			await Api.FetchSectionList(termId, "CS"); // test CS for now.
		}
	}
}
