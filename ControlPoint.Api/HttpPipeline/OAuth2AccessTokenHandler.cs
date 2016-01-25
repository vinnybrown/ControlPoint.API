using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ControlPoint.Api.Controllers;
using ControlPoint.Api.DataServices;

namespace ControlPoint.Api.HttpPipeline
{
	public class OAuth2AccessTokenHandler : DelegatingHandler
	{
		private const string HTTP_HEADER_AUTH = "Authorization";
		private const string HTTP_HEADER_AUTH_VAL_BEARER_PREFIX = "Bearer ";
		private static readonly OAuth2Service s_oauthService = new OAuth2Service();
		//private static readonly ILog s_log = LogManager.GetLogger(typeof(OAuth2AccessTokenHandler));

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// If there's a Bearer token in the Authorization HTTP request header, try converting it to a Member ID
			try
			{
				string accessToken = ReadRequestAccessToken(request);
				if (!string.IsNullOrEmpty(accessToken))
				{
					long memberId = s_oauthService.CheckIfTokenExists(accessToken);
					request.Properties.Add(BaseApiController.REQ_PROP_USER_ID, memberId);
				}
			}
			catch (Exception ex)
			{
				//s_log.Error("Unable to decode access token", ex);
			}

			// Proceed to controller regardless of whether we have a valid token
			return base.SendAsync(request, cancellationToken).ContinueWith(task =>
			{
				var response = task.Result;
				return response;
			});
		}

		protected string ReadRequestAccessToken(HttpRequestMessage request)
		{
			IEnumerable<string> values;
			if (request.Headers.TryGetValues(HTTP_HEADER_AUTH, out values))
			{
				foreach (string authValue in values)
				{
					if (authValue != null && authValue.StartsWith(HTTP_HEADER_AUTH_VAL_BEARER_PREFIX))
					{
						return authValue.Substring(HTTP_HEADER_AUTH_VAL_BEARER_PREFIX.Length);
					}
				}
			}

			return null;
		}
	}
}