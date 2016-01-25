using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ControlPoint.Api.Models
{
	public class ErrorException
	{
		public string ClassName { get; set; }
		public string Message { get; set; }
		public string StackTrace { get; set; }
		public ErrorException InnerException { get; set; }
	}
}