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

        // GET: Catalog
		[Route("")]
		[ResponseType(typeof(IEnumerable<CourseViewModel>))]
		public IHttpActionResult Get()
        {
			// Get all of the courses, convert to viewmodel.
			IEnumerable<CourseViewModel> allcourses = _Db.Courses.ToList().Select(x => x.ToViewModel());

			// Return w/ Ok status code.
			return Ok<IEnumerable<CourseViewModel>>(allcourses);
        }

        // GET: Catalog/[CourseSubject][CourseNumber] (ex. Catalog/MA261)
		[Route("{course}")]
		[ResponseType(typeof (IEnumerable<CourseViewModel>))]
		public IHttpActionResult Get(String course)
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

		//GET: Catalog/[CourseSubject][CourseNumber]/[ClassGUID] 
		[Route("{course}/{classGUID}")]
		[ResponseType(typeof(IEnumerable<ClassViewModel>))]
		public IHttpActionResult Get(String course, String classGUID)
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

		//GET: Catalog/[CourseSubject][CourseNumber]/[ClassGUID]/[SectionGUID]
		[Route("{course}/{classGUID}/{sectionGUID}")]
		[ResponseType(typeof(IEnumerable<ClassViewModel>))]
		public IHttpActionResult Get(String course, String classGUID, String sectionGUID)
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
