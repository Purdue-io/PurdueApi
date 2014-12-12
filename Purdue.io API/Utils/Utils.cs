using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace PurdueIo.Utils
{
	public class Utilities
	{
		private const string COURSE_SUBJECT_CAPTURE_GROUP_NAME = "subject";
		private const string COURSE_NUMBER_CAPTURE_GROUP_NAME = "number";
		private const string TERM_CAPTURE_GROUP_NAME = "term";

		//Regex strings
		private const string COURSE_SUBJECT_NUMBER_REGEX = @"\A(?<" + COURSE_SUBJECT_CAPTURE_GROUP_NAME + @">[A-Za-z]+)(?<" + COURSE_NUMBER_CAPTURE_GROUP_NAME + @">\d{3}(?:00)?)\z";
		private const string TERM_REGEX = @"\A(?<" + TERM_CAPTURE_GROUP_NAME + @">\d{6})\z";

		/// <summary>
		/// Helper function used to parse course input in the format of [CourseSubject][CourseNumber] (ex. MA261).  Returns null if the input is not in the format.
		/// </summary>
		/// <param name="course"></param>
		/// <returns></returns>
		public static Tuple<String, String> ParseCourse(String course)
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

		/// <summary>
		/// Helper function used to parse term inputs,
		/// returns the match is success, null otherwise
		/// </summary>
		/// <param name="term"></param>
		/// <returns></returns>
		public static String ParseTerm(String term)
		{
			Regex regex = new Regex(TERM_REGEX);
			Match match = regex.Match(term);

			if(match.Success)
			{
				return match.Groups[TERM_CAPTURE_GROUP_NAME].Value;
			}
			else
			{
				return null;
			}
		}
	}
}