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
        /// <summary>
        /// CatalogConnection used to send requests to MyPurdue
        /// </summary>
        private CatalogConnection catalogConnection = new CatalogConnection();

        /// <summary>
        /// Determines whether or not we have successfully authenticated
        /// </summary>
		public bool IsAuthenticated
        {
            get
            {

                return catalogConnection.IsAuthenticated;
            }
        }

        /// <summary>
        /// MyPurdue username for authenticated requests
        /// </summary>
		private string username = "";

        /// <summary>
        /// MyPurdue password for authenticated requests
        /// </summary>
		private string password = "";

        /// <summary>
        /// Constructs a new CatalogApi to retrieve information from MyPurdue
        /// </summary>
        /// <param name="u">Username for authenticated requests</param>
        /// <param name="p">Password for authenticated requests</param>
		public CatalogApi(string u = null, string p = null)
		{
			if (u != null)
			{
				username = u;
				password = p;
			}
		}

		// Methods meant to be accessed by users of the API
		#region API Methods
		public async Task<bool> Authenticate()
        {
            if (!IsAuthenticated)
            {
                return await catalogConnection.Authenticate(username, password);
            } else
            {
                return true;
            }
        }

        /// <summary>
        /// Resets the connection - resulting in cleared cookies.
        /// Good to fix things up when MyPurdue invalidates your session, or some other error state occurs.
        /// </summary>
        public void ResetConnection()
        {
            catalogConnection = new CatalogConnection();
        }

		/// <summary>
		/// Fetches the term list from myPurdue.
		/// </summary>
		/// <returns>A list of term objects.</returns>
		public async Task<ICollection<MyPurdueTerm>> FetchTermList()
		{
            var result = await catalogConnection.Request(CatalogConnection.HttpMethod.GET, "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched");
            var content = await result.Content.ReadAsStringAsync();
            return ParseContent<TermListParser, List<MyPurdueTerm>>(content);
		}

		/// <summary>
		/// Fetches a list of subjects for the specified term.
		/// </summary>
		/// <param name="termCode">myPurdue term code, e.g. 201510</param>
		/// <returns></returns>
		public async Task<ICollection<MyPurdueSubject>> FetchSubjectList(string termCode)
		{
            var request = await catalogConnection.Request(CatalogConnection.HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckgens.p_proc_term_date",
                new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("p_calling_proc", "bwckschd.p_disp_dyn_sched"),
                        new KeyValuePair<string, string>("p_term", termCode)
                    }),
                true,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_dyn_sched"
                );
            var requestContent = await request.Content.ReadAsStringAsync();

			return ParseContent<SubjectListParser, List<MyPurdueSubject>>(requestContent);
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
        /// Used to parse the result of a request using a IParser class.
        /// </summary>
        /// <typeparam name="T">An IParser that will take content and turn it into U</typeparam>
        /// <typeparam name="U">The type of data that the IParser will output</typeparam>
        /// <param name="content">Content to parse</param>
        /// <returns></returns>
        private U ParseContent<T, U>(string content) where T : IParser<U>, new()
        {
            // Parse the string using the specified parser.
            var parser = new T();
            U parseResults = parser.ParseHtml(content);
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

            var request = await catalogConnection.Request(CatalogConnection.HttpMethod.POST, "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_get_crse_unsec", postBody);
            var requestContent = await request.Content.ReadAsStringAsync();

            return ParseContent<SectionListParser, Dictionary<string,MyPurdueSection>>(requestContent);
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

            // Add our final request
            var request = await catalogConnection.Request(CatalogConnection.HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse_Advanced",
                postBody,
                true,
                "https://selfservice.mypurdue.purdue.edu/prod/bwskfcls.P_GetCrse");
            var requestContent = await request.Content.ReadAsStringAsync();

            return ParseContent<SectionDetailsParser, Dictionary<string, MyPurdueSection>>(requestContent);
		}

		/// <summary>
		/// Fetches the seats for a specific section by CRN
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="crn">The CRN number for the section you wish to fetch details on</param>
		/// <returns></returns>
		private async Task<MyPurdueSectionSeats> _FetchCrnSeats(string termCode, string crn)
		{
            var request = await catalogConnection.Request(CatalogConnection.HttpMethod.GET,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_detail_sched?term_in=" + termCode + "&crn_in=" + crn,
                null,
                true,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckschd.p_disp_listcrse?term_in=" + termCode + "&subj_in=&crse_in=&crn_in=" + crn);

            var requestContent = await request.Content.ReadAsStringAsync();
			return ParseContent<SectionSeatsParser, MyPurdueSectionSeats>(requestContent);
		}

		/// <summary>
		/// Attempts to add the list of CRNs to the user's schedule for the specified term.
        /// HERE BE DRAGONS: This code is fairly untested.
		/// </summary>
		/// <param name="termCode">myPurdue code for the desired term, e.g. 201510</param>
		/// <param name="pin">User's registration PIN</param>
		/// <param name="crnList">The list of CRNs to add to the user's schedule</param>
		/// <returns></returns>
		private async Task _AddCrn(string termCode, string pin, List<string> crnList)
		{
            var termPinRequest = await catalogConnection.Request(CatalogConnection.HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwskfreg.P_AltPin",
                new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("term_in", termCode)
                    }));
            var termPinRequestContent = await termPinRequest.Content.ReadAsStringAsync();

            var pinRequest = await catalogConnection.Request(CatalogConnection.HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwskfreg.P_CheckAltPin",
                new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("pin", pin)
                    }));
            var pinRequestContent = await pinRequest.Content.ReadAsStringAsync();

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

            var addCrnRequest = await catalogConnection.Request(CatalogConnection.HttpMethod.POST,
                "https://selfservice.mypurdue.purdue.edu/prod/bwckcoms.P_Regs",
                postBody);
            var addCrnRequestContent = await addCrnRequest.Content.ReadAsStringAsync();

            ParseContent<AddDropParser, bool>(addCrnRequestContent);
        }

        /// <summary>
        /// Method to fetch a user's schedule
        /// HERE BE DRAGONS: This code is fairly untested.
        /// </summary>
        /// <returns>A dictionary with keys of term name, values of lists containing CRNs</returns>
        private async Task<Dictionary<string, List<string>>> _UserSchedule()
		{
            var scheduleRequest = await catalogConnection.Request(CatalogConnection.HttpMethod.GET, "https://selfservice.mypurdue.purdue.edu/prod/bwsksreg.p_active_regs");
            var scheduleRequestContent = await scheduleRequest.Content.ReadAsStringAsync();

            return ParseContent<UserScheduleParser, Dictionary<string, List<string>>>(scheduleRequestContent);
        }
        #endregion
    }
}
