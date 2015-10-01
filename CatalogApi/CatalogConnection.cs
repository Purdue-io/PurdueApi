using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CatalogApi
{
    /// <summary>
    /// A persistent connection to the MyPurdue course catalog.
    /// </summary>
    public class CatalogConnection
    {
        /// <summary>
        /// Enum value of implemented HTTP request methods
        /// </summary>
        public enum HttpMethod { GET, POST };

        /// <summary>
        /// True when this connection has an authenticated session
        /// </summary>
        public bool IsAuthenticated { get; private set; }

        /// <summary>
        /// Keeps track of cookies for all requests on this connection
        /// </summary>
        private CookieContainer cookies = new CookieContainer();

        /// <summary>
        /// Keeps track of the last page requested, used in 'Referrer' HTTP header
        /// </summary>
        private string referrer = "";

        /// <summary>
        /// HttpClient used by this connection
        /// </summary>
        private HttpClient httpClient;

        /// <summary>
        /// Constructs a catalog connection with no authentication.
        /// </summary>
        public CatalogConnection()
        {
            // Initialize the HttpClient
            HttpClientHandler handler = new HttpClientHandler()
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false
            };
            httpClient = new HttpClient(handler as HttpMessageHandler);

            // Pretend we're Chrome
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.99 Safari/537.36");
        }

        /// <summary>
        /// Attempts to initiate an authenticated session with MyPurdue
        /// </summary>
        /// <param name="username">MyPurdue username</param>
        /// <param name="password">MyPurdue password</param>
        public async Task<bool> Authenticate(string username, string password)
        {
            var loginForm = await Request(HttpMethod.GET, "https://www.purdue.edu/apps/account/cas/login?service=https%3A%2F%2Fwl.mypurdue.purdue.edu%2Fc%2Fportal%2Flogin");
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(await loginForm.Content.ReadAsStringAsync());
            HtmlNode docRoot = document.DocumentNode;
            var ltValue = docRoot.SelectSingleNode("//input[@name='lt']").GetAttributeValue("value", "");

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("lt", ltValue),
                new KeyValuePair<string, string>("execution", "e1s1"),
                new KeyValuePair<string, string>("_eventId", "submit"),
                new KeyValuePair<string, string>("submit", "Login")
            });
            
            HttpResponseMessage r = await Request(HttpMethod.POST, "https://www.purdue.edu/apps/account/cas/login?service=https%3A%2F%2Fwl.mypurdue.purdue.edu%2Fc%2Fportal%2Flogin", content);
            string result = await r.Content.ReadAsStringAsync();
            IsAuthenticated = !result.Contains("Authentication failed.");

            if (!IsAuthenticated)
            {
                return false;
            }

            // Authenticate with MyPurdue self-service
            var ssResult = await Request(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/static_resources/portal/jsp/ss_redir_lp5.jsp?pg=23", null, true, "https://wl.mypurdue.purdue.edu/");
            var ssResultContent = await ssResult.Content.ReadAsStringAsync();
            // TODO: Verify self-service login

            return IsAuthenticated;
        }

        /// <summary>
		/// Generates a basic request, providing POST body, following redirects, and storing cookies.
		/// </summary>
		/// <param name="url">URL to request</param>
		/// <param name="postContent">Optional HTTP POST body</param>
		/// <param name="followRedirect">Whether or not to follow redirects.</param>
		/// <returns>An HttpResponseMessage result.</returns>
		public async Task<HttpResponseMessage> Request(HttpMethod method, string url, FormUrlEncodedContent postContent = null, bool followRedirect = true, string requestReferrer = null)
        {
            System.Diagnostics.Debug.WriteLine(method.ToString() + " Request: " + url);

            if (requestReferrer != null)
            {
                referrer = requestReferrer;
            }

            System.Diagnostics.Debug.WriteLine("\tReferrer: " + referrer);
            if (referrer.Length > 0)
            {
                httpClient.DefaultRequestHeaders.Referrer = new Uri(referrer);
            }
            else
            {
                if (httpClient.DefaultRequestHeaders.Contains("Referrer"))
                {
                    httpClient.DefaultRequestHeaders.Remove("Referrer");
                }
            }

            // Print out our POST data
            if (postContent != null)
            {
                System.Diagnostics.Debug.WriteLine("\tPOST data:");
                var postString = await postContent.ReadAsStringAsync();
                postString = "\t\t" + postString.Replace("&", "\n\t\t");
                System.Diagnostics.Debug.WriteLine(postString);
            }

            // Print out all the cookies we're sending
            var cookiesToSend = cookies.GetCookies(new Uri(url));
            System.Diagnostics.Debug.WriteLine("\tOutgoing cookies:");
            foreach (var cookie in cookiesToSend)
            {
                System.Diagnostics.Debug.WriteLine("\t\t" + cookie.ToString());
            }

            System.Diagnostics.Debug.WriteLine("\t...");
            HttpResponseMessage result = null;
            switch (method)
            {
                case HttpMethod.POST:
                    result = await httpClient.PostAsync(url, postContent);
                    break;
                case HttpMethod.GET:
                    result = await httpClient.GetAsync(url);
                    break;
            }
            if (result == null)
            {
                throw new InvalidOperationException("\tNo request was made - most likely due to invalid HTTP method.");
            }

            System.Diagnostics.Debug.WriteLine("\tIncoming cookies:");
            IEnumerable<string> incomingCookies;
            if (result.Headers.TryGetValues("set-cookie", out incomingCookies))
            {
                foreach (var c in incomingCookies)
                {
                    System.Diagnostics.Debug.WriteLine("\t\t" + c);
                    if (c.StartsWith("SESSID") && !c.Contains("expires"))
                    {
                        var sessidCookie = new Cookie("SESSID", c.Substring(c.IndexOf('=') + 1));
                        sessidCookie.Domain = new Uri(url).Host;
                        sessidCookie.Path = "/";
                        cookies.Add(sessidCookie);
                    }
                    cookies.SetCookies(new Uri(url), c);
                }
            }

            if (followRedirect && result.Headers.Location != null)
            {
                System.Diagnostics.Debug.WriteLine("\tREDIRECT to " + result.Headers.Location.ToString());
                // All redirects are converted to GET
                result = await Request(HttpMethod.GET, result.Headers.Location.ToString(), null);
            }
            else
            {
                referrer = url;
            }
            return result;
        }
    }
}
