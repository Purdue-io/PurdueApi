using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PurdueIo.Models
{
	[Serializable]
	public class StudentAddCourseModel
	{
		public string termCode { get; set; }
		public string pin { get; set; }

		public List<string> crnList { get; set; }
	}
}