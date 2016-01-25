using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ControlPoint.Api.Models
{
	public class ErrorMessage
	{
		public string Message { get; set; }
		public int ErrorCode { get; set; }
		public ErrorException Exception { get; set; }
	}
}