using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ControlPoint.Api.Models
{
	public class OAuthTokenResponse
	{
		public string access_token { get; set; }

		public string token_type { get; set; }

		public string expires_in { get; set; }

		public string refresh_token { get; set; }

		public string scope { get; set; }

		public long MemberID { get; set; }
	}
}