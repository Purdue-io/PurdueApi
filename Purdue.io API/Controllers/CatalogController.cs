using PurdueIo.Models;
using PurdueIo.Models.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Http.Results;

namespace PurdueIo.Controllers
{
	[RoutePrefix("Catalog")]
    public class CatalogController : ApiController
    {
		//Regex capture group names
		private const string COURSE_SUBJECT_CAPTURE_GROUP_NAME	= "subject";
		private const string COURSE_NUMBER_CAPTURE_GROUP_NAME	= "number";

		//Regex strings
		private const string COURSE_SUBJECT_NUMBER_REGEX		= @"\A(?<" + COURSE_SUBJECT_CAPTURE_GROUP_NAME + @">[A-Za-z]+)(?<" + COURSE_NUMBER_CAPTURE_GROUP_NAME + @">\d{3}(?:00)?)\z";

		// This DB really should be instantiated on its own in each method... but that causes disposed problems.
		private ApplicationDbContext _Db = new ApplicationDbContext();

		// GET: Catalog/Courses
		[Route("Courses")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllCourses()
        {
			// Get all of the courses, convert to viewmodel.
			IEnumerable<CourseViewModel> allcourses = _Db.Courses.ToList().Select(x => x.ToViewModel());

			// Return w/ Ok status code.
			return Ok<IEnumerable<CourseViewModel>>(allcourses);
        }

		// GET: Catalog/Courses/[CourseSubject][CourseNumber] (ex. Catalog/MA261)
		[Route("Courses/{course}")]
		[ResponseType(typeof (IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetCourses(String course)
        {
			//Use regex to parse course input
			Tuple<String, String> courseTuple = this.ParseCourse(course);

			//If there are no matches, exit with error
			if(courseTuple == null)
			{
				//error, invalid format
				return BadRequest("Invalid course format.  Course should be subject abbrivation followed by the course number (ex. PHYS172 or PHYS172000).");
			}
			
			IEnumerable<CourseViewModel> selectedCourses = _Db.Courses
				.Where(
					x => 
						x.Number == courseTuple.Item2 && 
						x.Subject.Abbreviation == courseTuple.Item1
					).ToList()
					.Select(
						x => 
							x.ToViewModel()
					);

			return Ok<IEnumerable<CourseViewModel>>(selectedCourses);
        }

		//GET: Catalog/Courses/[CourseSubject][CourseNumber]/[ClassGUID] 
		[Route("Courses/{course}/{classGUID}")]
		[ResponseType(typeof(IEnumerable<ClassViewModel>))]
		public IHttpActionResult GetClasses(String course, String classGUID)
		{
			//Use regex to parse course input
			Tuple<String, String> courseTuple = this.ParseCourse(course);
			Guid classID;

			//If there are no matches, exit with error
			if (courseTuple == null)
			{
				//error, invalid format
				return BadRequest("Invalid course format.  Course should be subject abbrivation followed by the course number (ex. PHYS172 or PHYS172000).");
			}

			//If class GUID format is invalid, exit with error
			try
			{
				classID = Guid.Parse(classGUID);
			}
			catch
			{
				return BadRequest("Invalid class GUID format.");
			}

			IEnumerable <ClassViewModel> selectedClasses = _Db.Classes
				.Where(
					x => 
						x.Course.Number == courseTuple.Item2 && 
						x.Course.Subject.Abbreviation == courseTuple.Item1 && 
						x.ClassId == classID
					).ToList()
					.Select(
						x => 
							x.ToViewModel()
					);

			return Ok<IEnumerable<ClassViewModel>>(selectedClasses);
		}

		//GET: Catalog/Courses/[CourseSubject][CourseNumber]/[ClassGUID]/[SectionGUID]
		[Route("Courses/{course}/{classGUID}/{sectionGUID}")]
		[ResponseType(typeof(IEnumerable<ClassViewModel>))]
		public IHttpActionResult GetSections(String course, String classGUID, String sectionGUID)
		{
			//Use regex to parse course input
			Tuple<String, String> courseTuple = this.ParseCourse(course);
			Guid classID;
			Guid sectionID;

			//If course format is invalid, exit with error
			if (courseTuple == null)
			{
				//error, invalid format
				return BadRequest("Invalid course format.  Course should be subject abbrivation followed by the course number (ex. PHYS172 or PHYS172000).");
			}

			//If class GUID format is invalid, exit with error
			try
			{
				classID = Guid.Parse(classGUID);
			}
			catch
			{
				return BadRequest("Invalid class GUID format.");
			}

			//If section GUID format is invalid, exit with error
			try
			{
				sectionID = Guid.Parse(sectionGUID);
			}
			catch
			{
				return BadRequest("Invalid section GUID format.");
			}

			IEnumerable<SectionViewModel> selectedSections = _Db.Sections
				.Where(
					x =>
						x.Class.Course.Number == courseTuple.Item2 &&
						x.Class.Course.Subject.Abbreviation == courseTuple.Item1 &&
						x.Class.ClassId == classID &&
						x.SectionId == sectionID
					).ToList()
					.Select(
						x => 
							x.ToViewModel()
					);
			
			return Ok<IEnumerable<SectionViewModel>>(selectedSections);
		}

		// GET: Catalog/Subjects
		[Route("Subjects")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllSubjects()
		{
			return Ok();
		}

		// GET: Catalog/Subjects/[Subject] (ex. Catalog/Subjects/MA)
		[Route("Subjects/{subject}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetSubjects(String subject)
		{
			return Ok();
		}

		// GET: Catalog/Terms
		[Route("Terms")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllTerms()
		{
			return Ok();
		}

		// GET: Catalog/Terms/[Term] (ex. Catalog/Terms/Fall14)
		[Route("Terms/{term}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetTerms(String term)
		{
			return Ok();
		}

		// GET: Catalog/Campuses
		[Route("Campuses")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllCampuses()
		{
			return Ok();
		}

		// GET: Catalog/Campuses/[Campus] (ex. Catalog/Campuses/Purdue%20University%20West%20Lafayette)
		[Route("Campuses/{campus}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetCampuses(String campus)
		{
			return Ok();
		}

		// GET: Catalog/Buildings
		[Route("Buildings")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllBuildings()
		{
			return Ok();
		}

		// GET: Catalog/Buildings/[building] (ex. Catalog/Buildings/LWSN)
		[Route("Buildings/{building}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetBuildings(String building)
		{
			return Ok();
		}

		// GET: Catalog/Rooms
		[Route("Rooms")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllRooms()
		{
			return Ok();
		}

		// GET: Catalog/Rooms/[room] (ex. Catalog/Rooms/B160)
		[Route("Rooms/{room}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetRooms(String room)
		{
			return Ok();
		}

		// GET: Catalog/Instructors
		[Route("Instructors")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllInstructors()
		{
			return Ok();
		}

		// GET: Catalog/Instructors/[instructor] (ex. Catalog/Instructors/Hubert%20E%20Dunsmore)
		[Route("Instructors/{instructor}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetInstructors(String instructor)
		{
			return Ok();
		}

		// GET: Catalog/RegistrationStatuses
		[Route("RegistrationStatuses")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetAllRegistrationStatuses()
		{
			return Ok();
		}

		// GET: Catalog/RegistrationStatuses/[status] (ex. Catalog/RegistrationStatuses/Open)
		[Route("RegistrationStatuses/{status}")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult GetRegistrationStatuses(String status)
		{
			return Ok();
		}

		//Imcomplete Skeleton
		public IHttpActionResult Post()
		{
			return null;
		}

		/// <summary>
		/// Helper function used to parse course input in the format of [CourseSubject][CourseNumber] (ex. MA261).  Returns null if the input is not in the format.
		/// </summary>
		/// <param name="course"></param>
		/// <returns></returns>
		private Tuple<String, String> ParseCourse(String course)
		{
			//Init regex
			Regex regex = new Regex(COURSE_SUBJECT_NUMBER_REGEX);
			Match match = regex.Match(course);

			//If there are no matches, exit with error
			if (!match.Success)
			{
				//error, invalid format
				return null;
			}

			//Capture subject and number group from the string
			String courseSubject = match.Groups[COURSE_SUBJECT_CAPTURE_GROUP_NAME].Value;
			String courseNumber = match.Groups[COURSE_NUMBER_CAPTURE_GROUP_NAME].Value;

			//Add zeros to number if number is only 3 characters (ex. 390 -> 39000)
			if (courseNumber.Length == 3)
			{
				courseNumber += "00";
			}

			return new Tuple<string, string>(courseSubject, courseNumber);
		}
    }
}
