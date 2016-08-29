using PurdueIo.Models;
using PurdueIo.Utils;
using PurdueIoDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;

namespace PurdueIo.Controllers
{
	[EnableCors(origins: "*", headers: "*", methods: "*")]
	[RoutePrefix("Student")]
	[RequireHttps]
    public class StudentController : ApiController
    {
		private ApplicationDbContext _Db = new ApplicationDbContext();

		[Route("Authenticate")]
		[HttpGet]
		public IHttpActionResult GetAuthenticate()
		{
			string[] creds;
			try
			{
				creds = ParseAuthorization(Request);
			}
			catch(Exception e)
			{
				return BadRequest("Invalid header: " + e.ToString());
			}

			CatalogApi.CatalogApi api = new CatalogApi.CatalogApi(creds[0], creds[1]);

			//Checks to see if the credentials are correct
			bool correct = false;
			try
			{
				correct = api.Authenticate().Result;
			}
			catch (Exception e)
			{
				return BadRequest("And error occured trying to verify credentials: " + e.ToString());
			}

			if (!correct)
			{
				return BadRequest("Invalid Credentials");
			}

			return Ok("Authenticated");
		}

		[Route("Schedule")]
		[HttpGet]
		public async Task<IHttpActionResult> GetSchedule()
		{
			string[] creds;
			try
			{
				creds = ParseAuthorization(Request);
			}
			catch (Exception e)
			{
				return BadRequest("Invalid header: " + e.ToString());
			}

			CatalogApi.CatalogApi api = new CatalogApi.CatalogApi(creds[0], creds[1]);

            //Checks to see if the credentials are correct
            bool correct = false;
            try
            {
                correct = api.Authenticate().Result;
            }
            catch (Exception e)
            {
                return BadRequest("And error occured trying to verify credentials: " + e.ToString());
            }

            if (!correct)
            {
                return BadRequest("Invalid Credentials");
            }

            Dictionary<string, List<string>> sch;
			try
			{
				sch = await api.UserSchedule();
			}
			catch(Exception e)
			{
				return BadRequest("Error getting schedule "  + e.ToString());
			}

			//find the list of section guids
			Dictionary<string, IEnumerable<Guid>> returnSch = new Dictionary<string, IEnumerable<Guid>>();
			foreach (var key in sch.Keys) 
			{
				var list = sch[key].AsQueryable();
				
				returnSch.Add(key, _Db.Sections.Where(
										s =>	
											list.Contains(s.CRN) && 
											s.Class.Term.Name.ToLower() == key.ToLower()
										).Select(
											s => s.SectionId
											));				
			}

			return Ok(returnSch);
		}

		[Route("AddCrns")]
		[HttpPost]
		public async Task<IHttpActionResult> PostAddCrns(StudentAddCourseModel model)
		{
			string[] creds;
			try
			{
				creds = ParseAuthorization(Request);
			}
			catch (Exception e)
			{
				return BadRequest("Invalid header: " + e.ToString());
			}

			if(model == null)
			{
				return BadRequest("No body");
			}

			if(model.pin == null)
			{
				return BadRequest("No specified pin");
			}

			if(model.crnList == null)
			{
				return BadRequest("No specified CRNs");
			}
			CatalogApi.CatalogApi api = new CatalogApi.CatalogApi(creds[0], creds[1]);

			//Checks to see if the credentials are correct
			bool correct = false;
			try
			{
				correct = api.Authenticate().Result;
			}
			catch (Exception e)
			{
				return BadRequest("And error occured trying to verify credentials: " + e.ToString());
			}

			if (!correct)
			{
				return BadRequest("Invalid Credentials");
			}

			//Attemps to add the classes
			try
			{
				await api.AddCrn(model.termCode, model.pin, model.crnList.Split(new char[] { ',' }).ToList());
			}
			catch (Exception e)
			{
				return BadRequest("An exception occured: " + e);
			}

			//TODO: There is no error message if bogus PIN and CRN is given
			return Ok("Added");
		}

		[Route("DropCrns")]
		[HttpPost]
		public IHttpActionResult DropClass()
		{
			return null;
		}

        /// <summary>
        /// This endpoint is used to get up-to-date information on a particular CRN.
        /// Useful for monitoring for class openings and such.
        /// </summary>
        /// <param name="termCode">Term code (e.g. 201710)</param>
        /// <param name="crn">CRN of section (e.g. 42908)</param>
        /// <returns>Section object with latest seating information</returns>
        [Route("Crn/{termCode}/{crn}")]
        [HttpGet]
        public async Task<IHttpActionResult> GetCrn(string termCode, string crn)
        {
            string[] creds;
            try
            {
                creds = ParseAuthorization(Request);
            }
            catch (Exception e)
            {
                return BadRequest("Invalid header: " + e.ToString());
            }

            CatalogApi.CatalogApi api = new CatalogApi.CatalogApi(creds[0], creds[1]);

            //Checks to see if the credentials are correct
            bool correct = false;
            try
            {
                correct = await api.Authenticate();
            }
            catch (Exception e)
            {
                return BadRequest("An error occured trying to verify credentials: " + e.ToString());
            }

            if (!correct)
            {
                return BadRequest("Invalid credentials");
            }

            //Attemps to grab the up-to-date info
            try
            {
                var seats = await api.FetchSectionSeats(termCode, crn);
                using (var db = new ApplicationDbContext())
                {
                    var dbSection = db.Sections.AsNoTracking().SingleOrDefault(s => s.CRN == crn && s.Class.Term.TermCode == termCode);
                    if (dbSection == null)
                    {
                        dbSection = new PurdueIoDb.Catalog.Section()
                        {
                            CRN = crn
                        };
                    }
                    dbSection.Capacity = seats.Capacity;
                    dbSection.Enrolled = seats.Enrolled;
                    dbSection.RemainingSpace = seats.RemainingSpace;
                    dbSection.WaitlistCapacity = seats.WaitlistCapacity;
                    dbSection.WaitlistCount = seats.WaitlistCount;
                    dbSection.WaitlistSpace = seats.WaitlistSpace;
                    return Ok(dbSection);
                }
            }
            catch (Exception e)
            {
                return BadRequest("An exception occured: " + e);
            }
        }

		private string[] ParseAuthorization(HttpRequestMessage request)
		{
			var he = request.Headers;
			if(!he.Contains("Authorization"))
			{
				throw new  Exception("No authorization header");
			}

			string auth = request.Headers.Authorization.ToString();

			if(auth == null || auth.Length == 0 || ! auth.StartsWith("Basic"))
			{
				throw new Exception("Invalid authorization header");
			}

			string base64Creds = auth.Substring(6);
			string[] creds = Encoding.ASCII.GetString(Convert.FromBase64String(base64Creds)).Split(new char[] { ':' });

			if(creds.Length != 2 || string.IsNullOrEmpty(creds[0]) || string.IsNullOrEmpty(creds[1]))
			{
				throw new Exception("Invalid authorization credentials, missing either the username or password");
			}

			return creds;
		}

		//Not used anymore, it actually does not save any space or reduce work since
		//new code need to be written to handel the output
		//just keeping this here in case I later want to go back and abstract it
		private string Authenticate(CatalogApi.CatalogApi api)
		{
			bool correct = false;
			try
			{
				correct = api.Authenticate().Result;
			}
			catch (Exception e)
			{
				return "And error occured trying to verify credentials: " + e.ToString();
			}

			if (!correct)
			{
				return "Invalid Credentials";
			}

			return null;
		}
    }
}
