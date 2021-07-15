using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace PurdueIo.Scraper.Connections
{
    // Implementation of IMyPurdueConnection that fetches actual data from MyPurdue via HTTPS calls
    public class MyPurdueConnection : IMyPurdueConnection
    {
        // Enum value of implemented HTTP request methods
        private enum HttpMethod { GET, POST };

        // Username used to authenticate with MyPurdue
        private readonly string username;

        // Password used to authenticate with MyPurdue
        private readonly string password;

        // Keeps track of cookies for all requests on this connection
        private CookieContainer cookies = new CookieContainer();

        // Keeps track of the last page requested, used in 'Referrer' HTTP header
        private string referrer = "";

        // HttpClient used by this connection to communicate with MyPurdue
        private HttpClient httpClient;

        // Attempts to open a new authenticated connection to MyPurdue,
        // throws if authentication fails
        public static async Task<MyPurdueConnection> CreateAndConnectAsync(string username,
            string password)
        {
            var connection = new MyPurdueConnection(username, password);
            if (await connection.Authenticate())
            {
                return connection;
            }
            else
            {
                throw new ApplicationException(
                    "Could not authenticate to MyPurdue with supplied username and password.");
            }
        }

        public async Task<string> GetTermListPageAsync()
        {
            var result = await Request(HttpMethod.GET,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
            var content = await result.Content.ReadAsStringAsync();
            return content;
        }

        public async Task<string> GetSubjectListPageAsync(string termCode)
        {
            var request = await Request(HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckgens.p_proc_term_date",
                new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("p_calling_proc",
                            "bwckschd.p_disp_dyn_sched"),
                        new KeyValuePair<string, string>("p_term", termCode)
                    }),
                true,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
            var content = await request.Content.ReadAsStringAsync();
            return content;
        }

        public async Task<string> GetSectionListPageAsync(string termCode, string subjectCode)
        {
            var postBody = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("term_in", termCode),
                new KeyValuePair<string, string>("sel_subj", "dummy"),
                new KeyValuePair<string, string>("sel_day", "dummy"),
                new KeyValuePair<string, string>("sel_schd", "dummy"),
                new KeyValuePair<string, string>("sel_insm", "dummy"),
                new KeyValuePair<string, string>("sel_camp", "dummy"),
                new KeyValuePair<string, string>("sel_levl", "dummy"),
                new KeyValuePair<string, string>("sel_sess", "dummy"),
                new KeyValuePair<string, string>("sel_instr", "dummy"),
                new KeyValuePair<string, string>("sel_ptrm", "dummy"),
                new KeyValuePair<string, string>("sel_attr", "dummy"),
                new KeyValuePair<string, string>("sel_subj", subjectCode),
                new KeyValuePair<string, string>("sel_crse", "%"),
                new KeyValuePair<string, string>("sel_title", ""),
                new KeyValuePair<string, string>("sel_schd", "%"),
                new KeyValuePair<string, string>("sel_from_cred", ""),
                new KeyValuePair<string, string>("sel_to_cred", ""),
                new KeyValuePair<string, string>("sel_camp", "%"),
                new KeyValuePair<string, string>("sel_ptrm", "%"),
                new KeyValuePair<string, string>("sel_instr", "%"),
                new KeyValuePair<string, string>("sel_sess", "%"),
                new KeyValuePair<string, string>("sel_attr", "%"),
                new KeyValuePair<string, string>("begin_hh", "0"),
                new KeyValuePair<string, string>("begin_mi", "0"),
                new KeyValuePair<string, string>("begin_ap", "a"),
                new KeyValuePair<string, string>("end_hh", "0"),
                new KeyValuePair<string, string>("end_mi", "0"),
                new KeyValuePair<string, string>("end_ap", "a"),
            });

            var request = await Request(HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_get_crse_unsec", postBody);
            var content = await request.Content.ReadAsStringAsync();
            return content;
        }

        public async Task<string> GetSectionDetailsPageAsync(string termCode, string subjectCode)
        {
            var postBody = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("rsts", "dummy"),
                new KeyValuePair<string, string>("crn", "dummy"),
                new KeyValuePair<string, string>("term_in", termCode),
                new KeyValuePair<string, string>("sel_subj", "dummy"),
                new KeyValuePair<string, string>("sel_day", "dummy"),
                new KeyValuePair<string, string>("sel_schd", "dummy"),
                new KeyValuePair<string, string>("sel_insm", "dummy"),
                new KeyValuePair<string, string>("sel_camp", "dummy"),
                new KeyValuePair<string, string>("sel_levl", "dummy"),
                new KeyValuePair<string, string>("sel_sess", "dummy"),
                new KeyValuePair<string, string>("sel_instr", "dummy"),
                new KeyValuePair<string, string>("sel_ptrm", "dummy"),
                new KeyValuePair<string, string>("sel_attr", "dummy"),
                new KeyValuePair<string, string>("sel_subj", subjectCode),
                new KeyValuePair<string, string>("sel_crse", ""),
                new KeyValuePair<string, string>("sel_title", ""),
                new KeyValuePair<string, string>("sel_schd", "%"),
                new KeyValuePair<string, string>("sel_insm", "%"),
                new KeyValuePair<string, string>("sel_from_cred", ""),
                new KeyValuePair<string, string>("sel_to_cred", ""),
                new KeyValuePair<string, string>("sel_camp", "%"),
                new KeyValuePair<string, string>("sel_ptrm", "%"),
                new KeyValuePair<string, string>("sel_instr", "%"),
                new KeyValuePair<string, string>("sel_sess", "%"),
                new KeyValuePair<string, string>("sel_attr", "%"),
                new KeyValuePair<string, string>("begin_hh", "0"),
                new KeyValuePair<string, string>("begin_mi", "0"),
                new KeyValuePair<string, string>("begin_ap", "a"),
                new KeyValuePair<string, string>("end_hh", "0"),
                new KeyValuePair<string, string>("end_mi", "0"),
                new KeyValuePair<string, string>("end_ap", "a"),
                new KeyValuePair<string, string>("SUB_BTN", "Section Search"),
                new KeyValuePair<string, string>("path", "1"),
            });

            var request = await Request(HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse_Advanced",
                postBody,
                true,
                "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse");
            var content = await request.Content.ReadAsStringAsync();
            return content;
        }

        private MyPurdueConnection(string username, string password)
        {
            this.username = username;
            this.password = password;

            var httpHandler = new HttpClientHandler()
            {
                CookieContainer = cookies, // MyPurdue stores a lot of state in cookies - we need
                                           // to persist them to avoid upsetting it
                AllowAutoRedirect = false, // We'll handle redirects by ourselves
            };
            httpClient = new HttpClient(httpHandler as HttpMessageHandler);

            // Pretend we're Chrome
            httpClient.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
            httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
            httpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) " + 
                "Chrome/45.0.2454.99 Safari/537.36");
        }

        private async Task<bool> Authenticate()
        {
            var loginForm = await Request(HttpMethod.GET,
                "https://www.purdue.edu/apps/account/cas/login" + 
                "?service=https%3A%2F%2Fwl.mypurdue.purdue.edu%2Fc%2Fportal%2Flogin");
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(await loginForm.Content.ReadAsStringAsync());
            HtmlNode docRoot = document.DocumentNode;
            var ltValue = docRoot
                .SelectSingleNode("//input[@name='lt']")
                .GetAttributeValue("value", "");

            FormUrlEncodedContent content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password),
                new KeyValuePair<string, string>("lt", ltValue),
                new KeyValuePair<string, string>("execution", "e1s1"),
                new KeyValuePair<string, string>("_eventId", "submit"),
                new KeyValuePair<string, string>("submit", "Login")
            });
            
            HttpResponseMessage r = await Request(HttpMethod.POST,
                "https://www.purdue.edu/apps/account/cas/login" + 
                "?service=https%3A%2F%2Fwl.mypurdue.purdue.edu%2Fc%2Fportal%2Flogin", content);
            string result = await r.Content.ReadAsStringAsync();
            bool isAuthenticated = !result.Contains("Authentication failed.");

            if (!isAuthenticated)
            {
                return false;
            }

            // Authenticate with MyPurdue self-service
            var ssResult = await Request(HttpMethod.GET,
                "https://wl.mypurdue.purdue.edu/static_resources/portal/jsp/ss_redir_lp5.jsp?pg=23",
                null, true, "https://wl.mypurdue.purdue.edu/");
            var ssResultContent = await ssResult.Content.ReadAsStringAsync();
            // TODO: Verify self-service login

            return isAuthenticated;
        }

        // Generates a basic request, providing POST body, following redirects, and storing cookies.
        private async Task<HttpResponseMessage> Request(HttpMethod method, string url,
            FormUrlEncodedContent postContent = null, bool followRedirect = true,
            string requestReferrer = null)
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
                throw new InvalidOperationException(
                    "No request was made - most likely due to invalid HTTP method.");
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
                        var sessidCookieValue = c.Substring((c.IndexOf('=') + 1),
                            (c.IndexOf(';') - c.IndexOf('=') - 1));
                        var sessidCookie = new Cookie("SESSID", sessidCookieValue);
                        sessidCookie.Domain = new Uri(url).Host;
                        sessidCookie.Path = "/";
                        cookies.Add(sessidCookie);
                    }
                    cookies.SetCookies(new Uri(url), c);
                }
            }

            if (followRedirect && (result.Headers.Location != null))
            {
                System.Diagnostics.Debug.WriteLine(
                    "\tREDIRECT to " + result.Headers.Location.ToString());
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