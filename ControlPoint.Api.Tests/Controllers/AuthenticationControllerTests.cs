using System.Net.Http;
using ControlPoint.Api.Models;
using NUnit.Framework;
using System.Collections.Generic;

namespace ControlPoint.Api.Tests.Controllers
{
	[TestFixture]
	public class AuthenticationControllerTests : BaseControllerTest
	{
		[Test]
		public void Post_ValidCredentials_ReturnsOk()
		{
			var resp = ApiClient.ExecuteRequest<OAuthTokenResponse>(HttpMethod.Post, "/authentication", null, new LoginParams
			{
				UserName = "vin",
				Password = "a1111111"
			});
			Assert.IsNotNull(resp);
			Assert.IsNotNullOrEmpty(resp.access_token);
		}
	}
}
