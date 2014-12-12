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
		//comma separated
		public string crnList { get; set; }
	}
}