using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PurdueIo.Scraper.Connections
{
    // Implementation of IMyPurdueConnection that fetches actual data from MyPurdue via HTTPS calls
    public class MyPurdueConnection : IMyPurdueConnection
    {
        // Enum value of implemented HTTP request methods
        private enum HttpMethod { GET, POST };

        // Logger reference
        private readonly ILogger<MyPurdueConnection> logger;

        // Keeps track of cookies for all requests on this connection
        private readonly CookieContainer cookies = new();

        // Keeps track of the last page requested, used in 'Referrer' HTTP header
        private string referrer = "";

        // HttpClient used by this connection to communicate with MyPurdue
        private readonly HttpClient httpClient;

        // How many request attempts should be made before failure
        private const int MAX_RETRIES = 6;

        // MyPurdue now throttles our requests, and only seems to let us through
        // after waiting a minimum of one minute.
        private const int THROTTLE_DELAY_BASE_MS = 60000;

        public MyPurdueConnection(ILogger<MyPurdueConnection> logger)
        {
            this.logger = logger;

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

        public async Task<string> GetTermListPageAsync()
        {
            for (int attempts = 0; attempts < MAX_RETRIES; ++attempts)
            {
                var result = await Request(HttpMethod.GET,
                    "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
                if (!result.IsSuccessStatusCode)
                {
                    this.logger.LogError("Received non-success status code '{}' on " +
                        "GetTermListPageAsync.", result.StatusCode);
                }
                else
                {
                    return await result.Content.ReadAsStringAsync();
                }
            }
            throw new ApplicationException(
                "Exceeded retries attempting to query MyPurdue term list");
        }

        public async Task<string> GetSubjectListPageAsync(string termCode)
        {
            for (int attempts = 0; attempts < MAX_RETRIES; ++attempts)
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
                if (!request.IsSuccessStatusCode)
                {
                    this.logger.LogError("Received non-success status code '{}' on " +
                        "GetSubjectListPageAsync.", request.StatusCode);
                }
                else
                {
                    var content = await request.Content.ReadAsStringAsync();
                    return content;
                }
            }
            throw new ApplicationException(
                "Exceeded retries attempting to query MyPurdue subject list");
        }

        public async Task<string> GetSectionListPageAsync(string termCode, string subjectCode)
        {
            for (int attempts = 0; attempts < MAX_RETRIES; ++attempts)
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
                    "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_get_crse_unsec",
                    postBody);

                if (!request.IsSuccessStatusCode)
                {
                    this.logger.LogError("Received non-success status code '{}' on " +
                        "GetSectionListPageAsync.", request.StatusCode);
                    continue;
                }

                var content = await request.Content.ReadAsStringAsync();
                if (content.Contains("We are sorry, but the site has received too many requests."))
                {
                    var retryDelayMs = THROTTLE_DELAY_BASE_MS + ExponentialRetryDelayMs(attempts);
                    this.logger.LogError("MyPurdue is throttling requests. Retrying in {}ms...",
                        retryDelayMs);
                    await Task.Delay(retryDelayMs);
                    continue;
                }
                return content;
            }
            throw new ApplicationException(
                "Exceeded retries attempting to query MyPurdue section list");
        }

        public async Task<string> GetSectionDetailsPageAsync(string termCode, string subjectCode)
        {
            for (int attempts = 0; attempts < MAX_RETRIES; ++attempts)
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
                if (!request.IsSuccessStatusCode)
                {
                    this.logger.LogError("Received non-success status code '{}' on " +
                        "GetSectionListPageAsync.", request.StatusCode);
                }
                else
                {
                    return await request.Content.ReadAsStringAsync();
                }
            }
            throw new ApplicationException(
                "Exceeded retries attempting to query MyPurdue section details");
        }

        // Generates a basic request, providing POST body, following redirects, and storing cookies.
        private async Task<HttpResponseMessage> Request(HttpMethod method, string url,
            FormUrlEncodedContent postContent = null, bool followRedirect = true,
            string requestReferrer = null)
        {
            logger.LogDebug("{} Request: {}", method.ToString(), url);

            if (requestReferrer != null)
            {
                referrer = requestReferrer;
            }

            logger.LogDebug("Referrer: {}", referrer);
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
                var postString = await postContent.ReadAsStringAsync();
                postString = postString.Replace("&", "\n\t\t");
                logger.LogDebug("POST data: \n{}", postString);
            }

            // Print out all the cookies we're sending
            var cookiesToSend = cookies.GetCookies(new Uri(url));
            logger.LogDebug("Outgoing cookies: \n{}",
                string.Join("\n", cookiesToSend.Select(c => c.ToString())));

            HttpResponseMessage result = null;
            for (int attempts = 0; attempts < MAX_RETRIES; ++attempts)
            {
                try
                {
                    switch (method)
                    {
                        case HttpMethod.POST:
                            result = await httpClient.PostAsync(url, postContent);
                            break;
                        case HttpMethod.GET:
                            result = await httpClient.GetAsync(url);
                            break;
                    }
                }
                catch (Exception e)
                {
                    logger.LogWarning("HTTP request exception, retrying ({} / {})\n{}",
                        (attempts + 1), MAX_RETRIES, e.ToString());
                    continue;
                }
                break;
            }
            if (result == null)
            {
                throw new InvalidOperationException(
                    "No request was made - most likely due to invalid HTTP method.");
            }

            if (result.Headers.TryGetValues("set-cookie", out IEnumerable<string> incomingCookies))
            {
                logger.LogDebug("Incoming cookies:\n{}", string.Join("\n", incomingCookies));
                foreach (var c in incomingCookies)
                {
                    if (c.StartsWith("SESSID") && !c.Contains("expires"))
                    {
                        var sessidCookieValue = c.Substring((c.IndexOf('=') + 1),
                            (c.IndexOf(';') - c.IndexOf('=') - 1));
                        var sessidCookie = new Cookie("SESSID", sessidCookieValue)
                        {
                            Domain = new Uri(url).Host,
                            Path = "/"
                        };
                        cookies.Add(sessidCookie);
                    }
                    cookies.SetCookies(new Uri(url), c);
                }
            }

            if (followRedirect && (result.Headers.Location != null))
            {
                logger.LogDebug("Redirect to {}", result.Headers.Location.ToString());
                // All redirects are converted to GET
                result = await Request(HttpMethod.GET, result.Headers.Location.ToString(), null);
            }
            else
            {
                referrer = url;
            }
            return result;
        }

        private static int ExponentialRetryDelayMs(int retryNumber)
        {
            return (int)(Math.Pow(2, retryNumber) * 1000);
        }
    }
}