using CatalogApi.Models;
using HtmlAgilityPack;
using PurdueIoDb.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CatalogApi.Parsers;

namespace CatalogApi
{
	public class CatalogApi
	{
		public enum HttpMethod { GET, POST };
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

		// Methods meant to be accessed by users of the API
		#region API Methods
		/// <summary>
		/// A way for API users to determine whether the credentials are valid.
		/// </summary>
		/// <returns></returns>
		public async Task<bool> HasValidCredentials()
		{
			return await Authenticate();
		}

		/// <summary>
		/// Fetches the term list from myPurdue.
		/// </summary>
		/// <returns>A list of term objects.</returns>
		public async Task<ICollection<MyPurdueTerm>> FetchTermList()
		{
			// Set up the request list...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched", null, initialReferrer),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched", null, null)
			};
			return await RequestParse<TermListParser, List<MyPurdueTerm>>(requests);
		}

		/// <summary>
		/// Fetches a list of subjects for the specified term.
		/// </summary>
		/// <param name="termCode">myPurdue term code, e.g. 201510</param>
		/// <returns></returns>
		public async Task<ICollection<MyPurdueSubject>> FetchSubjectList(string termCode)
		{
			// Set up the request list...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched", null, initialReferrer),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwckgens.p_proc_term_date", new FormUrlEncodedContent(new[] 
					{
						new KeyValuePair<string, string>("p_calling_proc", "bwckschd.p_disp_dyn_sched"),
						new KeyValuePair<string, string>("p_term", termCode)
					}), "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched")
			};

			return await RequestParse<SubjectListParser, List<MyPurdueSubject>>(requests);
		}

		/// <summary>
		/// Fetches all section information for the subject in the term provided
		/// by merging data from _FetchSectionList and _FetchSectionDetails
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="subjectCode">code for the desired subject, e.g. CS</param>
		/// <returns>Dictionary of sections keyed by CRN</returns>
		public async Task<Dictionary<string, MyPurdueSection>> FetchSections(string termCode, string subjectCode)
		{
			var sectionList = await _FetchSectionList(termCode, subjectCode);
			var sectionDetails = await _FetchSectionDetails(termCode, subjectCode);

			// Fill in the missing data on the list from the details.
			foreach (var sectionPair in sectionList)
			{
				var crn = sectionPair.Key;
				var section = sectionPair.Value;
				var detailedSection = sectionDetails[crn];

				// Update missing info
				section.Capacity = detailedSection.Capacity;
				section.Enrolled = detailedSection.Enrolled;
				section.RemainingSpace = detailedSection.RemainingSpace;
				section.WaitlistCapacity = detailedSection.WaitlistCapacity;
				section.WaitlistCount = detailedSection.WaitlistCount;
				section.WaitlistSpace = detailedSection.WaitlistSpace;
				section.Type = detailedSection.Type;
				section.SectionCode = detailedSection.SectionCode;
				section.CampusCode = detailedSection.CampusCode;

				// Update missing meeting info
				for (int i = 0; i < section.Meetings.Count; i++)
				{
					section.Meetings[i].BuildingCode = detailedSection.Meetings[i].BuildingCode;
					section.Meetings[i].Type = detailedSection.Meetings[i].Type;
				}
			}

			return sectionList;
		}

		/// <summary>
		/// Fetches the seat information for a single section.
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="crn">CRN for section to fetch information for</param>
		/// <returns>MyPurdueSectionSeats object for specific section</returns>
		public async Task<MyPurdueSectionSeats> FetchSectionSeats(string termCode, string crn)
		{
			var sectionList = await _FetchCrnSeats(termCode, crn);
			return sectionList;
		}

		/// <summary>
		/// Attempts to add the list of CRNs given to the user's schedule for the given term.
		/// This method will throw an exception if there are registration errors.
		/// </summary>
		/// <param name="termCode">myPurdue code for the term to add classes to, e.g. 201510</param>
		/// <param name="pin">User's registration PIN</param>
		/// <param name="crnList">List of CRNs to add to the user's schedule</param>
		/// <returns></returns>
		public async Task AddCrn(string termCode, string pin, List<string> crnList)
		{
			await _AddCrn(termCode, pin, crnList);
		}

		/// <summary>
		/// Fetches the current user's schedule
		/// </summary>
		/// <returns>A key-value list, key = term name (Fall 2014), value = list of CRNs for that term</returns>
		public async Task<Dictionary<string, List<string>>> UserSchedule()
		{
			return await _UserSchedule();
		}

		#endregion

		// Methods reserved for internal use by the API object
		#region Private Methods

		/// <summary>
		/// Generates a basic request, providing POST body, following redirects, and storing cookies.
		/// </summary>
		/// <param name="url">URL to request</param>
		/// <param name="post_content">Optional HTTP POST body</param>
		/// <param name="follow_redirect">Whether or not to follow redirects.</param>
		/// <returns>An HttpResponseMessage result.</returns>
		private async Task<HttpResponseMessage> Request(HttpMethod method, string url, FormUrlEncodedContent post_content = null, Boolean follow_redirect = true)
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
			HttpResponseMessage result = null;
			switch (method)
			{
				case HttpMethod.POST:
					result = await c.PostAsync(url, post_content);
					break;
				case HttpMethod.GET:
					result = await c.GetAsync(url);
					break;
			}
			if (result == null)
			{
				throw new InvalidOperationException("No request was made - most likely due to invalid HTTP method.");
			}

			referrer = url;
			if (follow_redirect && result.Headers.Location != null)
			{
				System.Diagnostics.Debug.WriteLine("Referrer detected. Pursuing...");
				result = await Request(method, result.Headers.Location.ToString(), post_content);
			}
			return result;
		}

		/// <summary>
		/// Authenticates against myPurdue, storing required cookies for future requests.
		/// </summary>
		/// <returns></returns>
		private async Task<bool> Authenticate()
		{
			FormUrlEncodedContent content = new FormUrlEncodedContent(new[] 
            {
                new KeyValuePair<string, string>("pass", password),
				new KeyValuePair<string, string>("user", username),
				new KeyValuePair<string, string>("uuid", "0xACA021")
			});

			await Request(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/home/loginf");
			await Request(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/home/displaylogin"); //Should set a ton of cookies ...
			HttpResponseMessage r = await Request(HttpMethod.POST, "https://wl.mypurdue.purdue.edu/cp/home/login", content);
			string result = await r.Content.ReadAsStringAsync();
			return (result.Contains("loginok.html"));
		}

		/// <summary>
		/// This method represents an entire request, response, and parse job.
		/// It takes a list of URIs, authenticates, hits each URL whilst preserving cookies and referrer,
		/// then parses the content of the last URI in the list with the specified IParser.
		/// </summary>
		/// <typeparam name="T">IParser type to use for parsing</typeparam>
		/// <typeparam name="U">Return type expected from IParser</typeparam>
		/// <param name="requests">An array of tuples, item1 being the URL, item2 being the optional post body. item3 being the referrer</param>
		/// <returns>Result from specified IParser</returns>
		private async Task<U> RequestParse<T, U>(List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>> requests) where T : IParser<U>, new()
		{
			// Clear cookies and authenticate.
			cookies = new CookieContainer();
			var authResult = await Authenticate();
			if (!authResult)
			{
				throw new ApplicationException("Could not authenticate to myPurdue with the provided credentials. Username: " + username);
			}

			// Go through each URL in the list.
			string result = "";
			foreach (var request in requests)
			{
				if (request.Item4 != null && request.Item4.Length > 0)
				{
					referrer = request.Item4;
				}
				HttpResponseMessage r = await Request(request.Item1, request.Item2, request.Item3);
				result = await r.Content.ReadAsStringAsync();
			}

			// Parse the result from the last URL in the list.
			var parser = new T();
			U parseResults = parser.ParseHtml(result);
			return parseResults;
		}

		/// <summary>
		/// This method fetches the preliminary section data for a particular subject in a term.
		/// The other half of the section data must be fetched by _FetchSectionDetails
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="subjectCode">code for the desired subject, e.g. CS</param>
		/// <returns>Dictionary of sections keyed by CRN</returns>
		private async Task<Dictionary<string,MyPurdueSection>> _FetchSectionList(string termCode, string subjectCode)
		{
			// Set up the request list...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched", null, initialReferrer),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwckgens.p_proc_term_date", new FormUrlEncodedContent(new[] 
					{
						new KeyValuePair<string, string>("p_calling_proc", "bwckschd.p_disp_dyn_sched"),
						new KeyValuePair<string, string>("p_term", termCode)
					}), null)
			};

			// Construct our "query"
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

			// Add our final request
			requests.Add(new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_get_crse_unsec", postBody, null));

			return await RequestParse<SectionListParser, Dictionary<string,MyPurdueSection>>(requests);
		}

		/// <summary>
		/// This method fetches the detailed section data for a particular subject in a term.
		/// The other half of the section data must be fetched by _FetchSectionList
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="subjectCode">code for the desired subject, e.g. CS</param>
		/// <returns>Dictionary of sections keyed by CRN</returns>
		private async Task<Dictionary<string,MyPurdueSection>> _FetchSectionDetails(string termCode, string subjectCode)
		{
			// Set up the request list ...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_LOOKCLASS";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_LOOKCLASS", null, initialReferrer)
			};

			// Construct our "query"
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

			referrer = "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse";

			// Add our final request
			requests.Add(new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse_Advanced", postBody, referrer));

			return await RequestParse<SectionDetailsParser, Dictionary<string, MyPurdueSection>>(requests);
		}

		/// <summary>
		/// Fetches the seats for a specific section by CRN
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="crn">The CRN number for the section you wish to fetch details on</param>
		/// <returns></returns>
		private async Task<MyPurdueSectionSeats> _FetchCrnSeats(string termCode, string crn)
		{
			// Set up the request list ...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_LOOKCLASS";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_LOOKCLASS", null, initialReferrer),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_detail_sched?term_in=" + termCode + "&crn_in=" + crn, null, "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_listcrse?term_in=" + termCode + "&subj_in=&crse_in=&crn_in=" + crn)
			};

			return await RequestParse<SectionSeatsParser, MyPurdueSectionSeats>(requests);
		}

		/// <summary>
		/// Attempts to add the list of CRNs to the user's schedule for the specified term.
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="pin">User's registration PIN</param>
		/// <param name="crnList">The list of CRNs to add to the user's schedule</param>
		/// <returns></returns>
		private async Task _AddCrn(string termCode, string pin, List<string> crnList)
		{
			// Set up the request list ...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_ADDDROP";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_ADDDROP", null, initialReferrer),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://selfservice.mypurdue.purdue.edu/prod/bwskfreg.P_AltPin", null, null),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwskfreg.P_AltPin", new FormUrlEncodedContent(new[] 
					{
						new KeyValuePair<string, string>("term_in", termCode)
					}), null),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwskfreg.P_CheckAltPin", new FormUrlEncodedContent(new[] 
					{
						new KeyValuePair<string, string>("pin", pin)
					}), null)
			};

			// Construct post body ...
			var bodyFields = new List<KeyValuePair<string, string>>()
			{
				new KeyValuePair<string, string>("term_in", termCode),
				new KeyValuePair<string, string>("RSTS_IN", "DUMMY"),
				new KeyValuePair<string, string>("assoc_term_in", "DUMMY"),
				new KeyValuePair<string, string>("CRN_IN", "DUMMY"),
				new KeyValuePair<string, string>("start_date_in", "DUMMY"),
				new KeyValuePair<string, string>("end_date_in", "DUMMY"),
				new KeyValuePair<string, string>("SUBJ", "DUMMY"),
				new KeyValuePair<string, string>("CRSE", "DUMMY"),
				new KeyValuePair<string, string>("SEC", "DUMMY"),
				new KeyValuePair<string, string>("LEVL", "DUMMY"),
				new KeyValuePair<string, string>("CRED", "DUMMY"),
				new KeyValuePair<string, string>("GMOD", "DUMMY"),
				new KeyValuePair<string, string>("TITLE", "DUMMY"),
				new KeyValuePair<string, string>("MESG", "DUMMY"),
				new KeyValuePair<string, string>("REG_BTN", "DUMMY"),
				new KeyValuePair<string, string>("MESG", "DUMMY"),
			};

			foreach (string crn in crnList)
			{
				bodyFields.Add(new KeyValuePair<string, string>("RSTS_IN", "RW"));
				bodyFields.Add(new KeyValuePair<string, string>("CRN_IN", crn));
				bodyFields.Add(new KeyValuePair<string, string>("assoc_term_in", ""));
				bodyFields.Add(new KeyValuePair<string, string>("start_date_in", ""));
				bodyFields.Add(new KeyValuePair<string, string>("end_date_in", ""));
			}

			bodyFields.Add(new KeyValuePair<string, string>("regs_row", "0"));
			bodyFields.Add(new KeyValuePair<string, string>("wait_row", "0"));
			bodyFields.Add(new KeyValuePair<string, string>("add_row", "" + crnList.Count));
			bodyFields.Add(new KeyValuePair<string, string>("REG_BTN", "Submit Changes"));

			var postBody = new FormUrlEncodedContent(bodyFields.ToArray());
			requests.Add(new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwckcoms.P_Regs", postBody, null));

			await RequestParse<AddDropParser, bool>(requests);
		}

		/// <summary>
		/// Method to fetch a user's schedule
		/// </summary>
		/// <returns>A dictionary with keys of term name, values of lists containing CRNs</returns>
		private async Task<Dictionary<string, List<string>>> _UserSchedule()
		{
			// Set up the request list ...
			var initialReferrer = "https://wl.mypurdue.purdue.edu/render.UserLayoutRootNode.uP?uP_tparam=utf&utf=%2fcp%2fip%2flogin%3fsys%3dsctssb%26url%3dhttps://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_ACTIVEREG";
			var requests = new List<Tuple<HttpMethod, string, FormUrlEncodedContent, string>>()
			{
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://wl.mypurdue.purdue.edu/cp/ip/login?sys=sctssb&url=https://selfservice.mypurdue.purdue.edu/prod/tzwkwbis.P_CheckAgreeAndRedir?ret_code=STU_ACTIVEREG", null, initialReferrer),
				new Tuple<HttpMethod, string, FormUrlEncodedContent, string>(HttpMethod.GET, "https://selfservice.mypurdue.purdue.edu/prod/bwsksreg.p_active_regs", null, null)
			};

			return await RequestParse<UserScheduleParser, Dictionary<string, List<string>>>(requests);
		}
		#endregion
	}
}
