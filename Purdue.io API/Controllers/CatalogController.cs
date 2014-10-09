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
        /// <summary>
        /// Returns all offered courses.
        /// </summary>
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
        /// <summary>
        /// Returns data for a specific course.
        /// </summary>
        /// <param name="course"> The desired course to examine - for example, MA261 or CS18000.</param>
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
        /// <summary>
        /// Returns data for a specific class in a specific course.
        /// </summary>
        /// <param name="course"> The desired course to examine - for example, MA261 or CS18000.</param>
        /// <param name="classGUID"> The class specific GUID.</param>
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
        /// <summary>
        /// Returns data for a specific section of a specific class in a specific course.
        /// </summary>
        /// <param name="course"> The desired course to examine - for example, MA261 or CS18000.</param>
        /// <param name="classGUID"> The class specific GUID.</param>
        /// <param name="sectionGUID"> The section specific GUID.</param>
		[Route("Courses/{course}/{classGUID}/{sectionGUID}")]
		[ResponseType(typeof(IEnumerable<SectionViewModel>))]
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
        /// <summary>
        /// Returns all offered subjects.
        /// </summary>
		[Route("Subjects")]
		[ResponseType(typeof(IEnumerable<SubjectViewModel>))]
		public IHttpActionResult GetAllSubjects()
		{
			return Ok();
		}

		// GET: Catalog/Subjects/[Subject] (ex. Catalog/Subjects/MA)
        /// <summary>
        /// Returns data for a specific subject.
        /// </summary>
        /// <param name="subject"> The desired subject to examine - for example, MA or ENGL.</param>
		[Route("Subjects/{subject}")]
		[ResponseType(typeof(IEnumerable<SubjectViewModel>))]
		public IHttpActionResult GetSubjects(String subject)
		{
			return Ok();
		}

		// GET: Catalog/Terms
        /// <summary>
        /// Returns all terms Purdue offers classes.
        /// </summary>
		[Route("Terms")]
		[ResponseType(typeof(IEnumerable<TermViewModel>))]
		public IHttpActionResult GetAllTerms()
		{
			return Ok();
		}

		// GET: Catalog/Terms/[Term] (ex. Catalog/Terms/Fall14)
        /// <summary>
        /// Returns data for a specific term.
        /// </summary>
        /// <param name="term"> The desired term to examine - for example, Fall14 or Spring15.</param>
		[Route("Terms/{term}")]
		[ResponseType(typeof(IEnumerable<TermViewModel>))]
		public IHttpActionResult GetTerms(String term)
		{
			return Ok();
		}

		// GET: Catalog/Campuses
        /// <summary>
        /// Returns information about all Purdue campuses.
        /// </summary>
		[Route("Campuses")]
		[ResponseType(typeof(IEnumerable<CampusViewModel>))]
		public IHttpActionResult GetAllCampuses()
		{
			return Ok();
		}

		// GET: Catalog/Campuses/[Campus] (ex. Catalog/Campuses/Purdue%20University%20West%20Lafayette)
        /// <summary>
        /// Returns data for a specific Purdue campus.
        /// </summary>
        /// <param name="campus"> The desired campus to examine - for example, Purdue University West Lafayette.</param>
		[Route("Campuses/{campus}")]
		[ResponseType(typeof(IEnumerable<CampusViewModel>))]
		public IHttpActionResult GetCampuses(String campus)
		{
			return Ok();
		}

		// GET: Catalog/Buildings
        /// <summary>
        /// Returns information about all Purdue affiliated buildings.
        /// </summary>
		[Route("Buildings")]
		[ResponseType(typeof(IEnumerable<BuildingViewModel>))]
		public IHttpActionResult GetAllBuildings()
		{
			return Ok();
		}

		// GET: Catalog/Buildings/[building] (ex. Catalog/Buildings/LWSN)
        /// <summary>
        /// Returns data for a specific building.
        /// </summary>
        /// <param name="building"> The desired building shortcode to examine - for example, LWSN.</param>
		[Route("Buildings/{building}")]
		[ResponseType(typeof(IEnumerable<BuildingViewModel>))]
		public IHttpActionResult GetBuildings(String building)
		{
			return Ok();
		}

		// GET: Catalog/Rooms
        /// <summary>
        /// Returns information about all rooms.
        /// </summary>
		[Route("Rooms")]
		[ResponseType(typeof(IEnumerable<RoomViewModel>))]
		public IHttpActionResult GetAllRooms()
		{
			return Ok();
		}

		// GET: Catalog/Rooms/[room] (ex. Catalog/Rooms/B160)
        /// <summary>
        /// Returns data for a specific room.
        /// </summary>
        /// <param name="building"> The desired room to examine - for example, B160.</param>
		[Route("Rooms/{room}")]
		[ResponseType(typeof(IEnumerable<RoomViewModel>))]
		public IHttpActionResult GetRooms(String room)
		{
			return Ok();
		}

		// GET: Catalog/Instructors
        /// <summary>
        /// Returns information about all instructors.
        /// </summary>
		[Route("Instructors")]
		[ResponseType(typeof(IEnumerable<InstructorViewModel>))]
		public IHttpActionResult GetAllInstructors()
		{
			return Ok();
		}

		// GET: Catalog/Instructors/[instructor] (ex. Catalog/Instructors/Hubert%20E%20Dunsmore)
        /// <summary>
        /// Returns data for a specific instructor.
        /// </summary>
        /// <param name="instructor"> The desired instructor to examine - for example, Hubert E Dunsmore.</param>
		[Route("Instructors/{instructor}")]
		[ResponseType(typeof(IEnumerable<InstructorViewModel>))]
		public IHttpActionResult GetInstructors(String instructor)
		{
			return Ok();
		}

		// POST: Catalog/Search/Courses
        /// <summary>
        /// Search for courses with matching information, such as courses with 3 credit hours.
        /// </summary>
		[Route("Search/Courses")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult PostSearchCourses()
		{
			return Ok();
		}

		// POST: Catalog/Search/Classes
        /// <summary>
        /// Search for classes with matching information, such as classes offered in Fall14.
        /// </summary>
		[Route("Search/Classes")]
		[ResponseType(typeof(IEnumerable<ClassViewModel>))]
		public IHttpActionResult PostSearchClasses()
		{
			return Ok();
		}

		// POST: Catalog/Search/Sections
        /// <summary>
        /// Search for sections with matching information, such as sections taught by Hubert E Dunsmore.
        /// </summary>
		[Route("Search/Sections")]
		[ResponseType(typeof(IEnumerable<SectionViewModel>))]
		public IHttpActionResult PostSearchSections()
		{
			return Ok();
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
