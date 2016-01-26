using ControlPoint.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ControlPoint.Api.Controllers
{
    public class AuthenticationController : BaseApiController
    {
		[AllowAnonymous]
		[HttpPost]
		public HttpResponseMessage Post([FromBody]LoginParams p)
		{
			if(p == null)
			{
				return new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.BadRequest
				};
			}

			if(string.IsNullOrEmpty(p.UserName) || string.IsNullOrEmpty(p.Password))
			{
				return new HttpResponseMessage
				{
					StatusCode = HttpStatusCode.BadRequest
				};
			}

			//TODO: do authentication here
			string mockAccessToken = Guid.NewGuid().ToString();

			return new HttpResponseMessage
			{
				StatusCode = HttpStatusCode.OK,
				Version = new Version(2,0),
				Content = new JsonContent(new OAuthTokenResponse
				{
					access_token = mockAccessToken,
					scope = "api",
					token_type = "Bearer",
					refresh_token = string.Empty,
					expires_in = "30"
				})
			};
		}

	}
}
