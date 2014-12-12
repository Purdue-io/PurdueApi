using PurdueIo.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace PurdueIo.Controllers
{
	[RoutePrefix("Student")]
    public class StudentController : ApiController
    {
		[Route("GetSchedule")]
		[HttpGet]
		public IHttpActionResult GetSchedule()
		{
			return null;
		}

		[Route("AddCrns")]
		[HttpPost]
		public async Task<IHttpActionResult> PostAddCrns(StudentAddCourseModel model)
		{
			if(model == null)
			{
				return BadRequest("No body");
			}

			if(model.username == null)
			{
				return BadRequest("No specified username");
			}

			if(model.password == null)
			{
				return BadRequest("No specified password");
			}

			if(model.pin == null)
			{
				return BadRequest("No specified pin");
			}

			if(model.crnList == null)
			{
				return BadRequest("No specified CRNs");
			}
			CatalogApi.CatalogApi api = new CatalogApi.CatalogApi(model.username, model.password);

			//Checks to see if the credentials are correct
			bool correct = false;
			try
			{
				correct = await api.HasValidCredentials();
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
				await api.AddCrn(model.termCode, model.pin, model.crnList);
			}
			catch (Exception e)
			{
				return BadRequest("An exception occured: " + e.ToString());
			}

			//No code to return from the async task it just goes, yolo
			return Ok();
		}

		[Route("DropCrns")]
		[HttpPost]
		public IHttpActionResult DropClass()
		{
			return null;
		}


    }
}
