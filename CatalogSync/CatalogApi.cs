using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CatalogSync
{
	class CatalogApi
	{
		public static string default_username = "";
		public static string default_password = "";

		private string username = "";
		private string password = "";
		private string referrer = "";
		private CookieContainer cookies = new CookieContainer();

		public CatalogApi(string u = null, string p = null)
		{
			if (u == null)
			{
				username = CatalogApi.default_username;
				password = CatalogApi.default_password;
			}
			else
			{
				username = u;
				password = p;
			}
		}

		private async Task<HttpResponseMessage> Request(string url, FormUrlEncodedContent post_content = null, Boolean follow_redirect = true)
		{
			HttpClientHandler handler = new HttpClientHandler()
			{
				CookieContainer = cookies,
				AllowAutoRedirect = false
			};
			HttpClient c = new HttpClient(handler as HttpMessageHandler)
			{
				BaseAddress = new Uri(url)
			};
			if (referrer.Length > 0)
				c.DefaultRequestHeaders.Referrer = new Uri(referrer);
			System.Diagnostics.Debug.WriteLine("Navigating to '" + url + "...");
			HttpResponseMessage result = await c.PostAsync(url, post_content);
			referrer = url;
			
			if (follow_redirect && result.Headers.Location != null)
			{
				System.Diagnostics.Debug.WriteLine("Referrer detected. Pursuing...");
				result = await Request(result.Headers.Location.ToString());
			}
			return result;
		}
		public async Task<bool> HasValidCredentials()
		{
			return await Authenticate();
		}
		private async Task<bool> Authenticate()
		{

			FormUrlEncodedContent content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("pass", password),
				new KeyValuePair<string, string>("user", username),
				new KeyValuePair<string, string>("uuid", "0xACA021")
			});

			await Request("https://wl.mypurdue.purdue.edu/cp/home/loginf");
			await Request("https://wl.mypurdue.purdue.edu/cp/home/displaylogin"); //Should set a ton of cookies ...
			HttpResponseMessage r = await Request("https://wl.mypurdue.purdue.edu/cp/home/login", content);
			string result = await r.Content.ReadAsStringAsync();
			return (result.Contains("Login Successful"));
		}

		public async Task FetchTermList()
		{
			await Authenticate();
			referrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched";
			HttpResponseMessage t = await Request("https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
			string tr = await t.Content.ReadAsStringAsync();

			///prod/bwskfcls.p_sel_crse_search
			HttpResponseMessage s = await Request("https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
			string sr = await s.Content.ReadAsStringAsync();
			HtmlDocument document = new HtmlDocument();
			document.LoadHtml(sr);
			HtmlNode root = document.DocumentNode;
			HtmlNodeCollection termSelectNodes = root.SelectNodes("//select[@id='term_input_id'][1]/option");
			foreach (var node in termSelectNodes)
			{
				Console.WriteLine(node.Attributes["VALUE"].Value + ": " + node.NextSibling.InnerText);
			}
		}
	}
}
