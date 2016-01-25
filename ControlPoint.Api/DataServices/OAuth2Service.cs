using System;
using System.Net;
using System.Web.Security;
using ControlPoint.Api.DataAccess;
using ControlPoint.Api.Models;

namespace ControlPoint.Api.DataServices
{
	public class OAuth2Service
	{
		private const string DEFAULT_SCOPE = "api";
		private const string TOKEN_TYPE_BEARER = "Bearer";
		private const string AUTH_SYSTEM_NAME = "Mobile";
		private const int AUTH_SESSION_TIMEOUT = 0;
		readonly ControlPointDao _dao = new ControlPointDao();

		/// <summary>
		/// Verify incoming request for access token
		/// </summary>
		public static bool VerifyOAuthRequestTokenParameters(OAuthTokenRequest tokenRequest)
		{
			if (!string.IsNullOrEmpty(tokenRequest.grant_type) &&
				!tokenRequest.grant_type.Equals(OAuthConstants.ACCESS_TOKEN))
			{
				throw new ApiException(HttpStatusCode.BadRequest, "Missing required parameter:  grant_type");
			}

			if (!string.IsNullOrEmpty(tokenRequest.scope) && !tokenRequest.scope.Equals(DEFAULT_SCOPE))
			{
				throw new ApiException(HttpStatusCode.BadRequest, "The specified scope is invalid");
			}

			if (string.IsNullOrEmpty(tokenRequest.scope))
			{
				tokenRequest.scope = DEFAULT_SCOPE;
			}

			if (string.IsNullOrEmpty(tokenRequest.username))
			{
				throw new ApiException(HttpStatusCode.BadRequest, "Missing required parameter:  username");
			}

			if (string.IsNullOrEmpty(tokenRequest.password))
			{
				throw new ApiException(HttpStatusCode.BadRequest, "Missing required parameter:  password");
			}

			return true;
		}

		/// <summary>
		/// A method that executes refresh token flow
		/// </summary>
		public OAuthRefreshTokenResponse ExecuteRefreshTokenFlow(OAuthTokenRequest tokenRequest)
		{
			if (!string.IsNullOrEmpty(tokenRequest.scope) && !tokenRequest.scope.Equals(DEFAULT_SCOPE))
			{
				throw new ApiException(HttpStatusCode.BadRequest, "The specified scope is invalid");
			}

			if (string.IsNullOrEmpty(tokenRequest.refresh_token))
			{
				throw new ApiException(HttpStatusCode.BadRequest, "Missing required parameter:  refresh_token");
			}

			Guid accessToken = Guid.NewGuid();
			long memberId = RefreshMemberAuthorization(tokenRequest.refresh_token, accessToken.ToString());
			var oAuthTokenResponse = new OAuthRefreshTokenResponse
			{
				MemberID = memberId,
				access_token = accessToken.ToString(),
				token_type = TOKEN_TYPE_BEARER,
				scope = tokenRequest.scope
			};

			return oAuthTokenResponse;
		}

		/// <summary>
		/// A method that executes request token flow
		/// </summary>
		public OAuthTokenResponse ExecuteTokenFlow(OAuthTokenRequest tokenRequest)
		{
			VerifyOAuthRequestTokenParameters(tokenRequest);

			long retrievedMemberId = VerifyCredentials(tokenRequest.username, tokenRequest.password);

			Guid accessTokenGuid = Guid.NewGuid();
			Guid refreshTokenGuid = Guid.NewGuid();

			DateTime validFrom = DateTime.UtcNow;
			DateTime validTo = validFrom.AddDays(1);

			AddMemberAuthorization(accessTokenGuid, retrievedMemberId, tokenRequest.scope, validFrom, validTo);

			return new OAuthTokenResponse
			{
				access_token = accessTokenGuid.ToString(),
				expires_in = null,
				refresh_token = refreshTokenGuid.ToString(),
				scope = tokenRequest.scope,
				token_type = TOKEN_TYPE_BEARER,
				MemberID = retrievedMemberId
			};
		}

		/// <summary>
		/// Method that retrieves token by supplied refresh token
		/// </summary>
		private long RefreshMemberAuthorization(string refreshToken, string accessToken)
		{
			long memberId = 0;

			Guid refreshTokenGuid;
			if (!Guid.TryParse(refreshToken, out refreshTokenGuid))
			{
				throw new ApiException("The refresh token is invalid");
			}

			Guid accessTokenGuid;
			if (!Guid.TryParse(accessToken, out accessTokenGuid))
			{
				throw new ApiException("The access token is invalid");
			}

			//using (var dao = new CRMDao())
			//{
			//    memberId = dao.RefreshMemberAuthorization(accessTokenGuid, refreshTokenGuid);
			//}
			return memberId;
		}

		/// <summary>
		/// A method that inserts newly generated tokens in to database
		/// </summary>
		private void AddMemberAuthorization(Guid token, long userId, string scope, DateTime from, DateTime to)
		{
			_dao.SaveUserAuth(userId, token, scope, from, to);
		}

		/// <summary>
		/// Delete all client authorizations associated with supplied refresh token
		/// </summary>
		public void DeleteClientAuthorization(string refreshToken)
		{
			Guid refreshTokenGuid;
			if (!Guid.TryParse(refreshToken, out refreshTokenGuid))
			{
				throw new ApiException("The refresh token is invalid");
			}

			//using (var dao = new CRMDao())
			//{
			//    dao.DeleteClientAuthorization(refreshTokenGuid);
			//}
		}

		/// <summary>
		/// A method that verifies user credentials and returns member ID
		/// </summary>
		public static long VerifyCredentials(string username, string password)
		{
			long userId = 0;
			if (Membership.ValidateUser(username, password))
			{
				var user = Membership.GetUser(username, true);
				if (user != null && user.ProviderUserKey != null)
				{
					userId = (long)user.ProviderUserKey;
				}
			}
			return userId;
		}

		/// <summary>
		/// A method that checks if access token exists in the DB
		/// </summary>
		public long CheckIfTokenExists(string accessToken)
		{
			Guid accessTokenGuid;
			if (!Guid.TryParse(accessToken, out accessTokenGuid))
			{
				throw new ApiException("The access token is invalid");
			}

			// TODO: check data store to lookup userId based on access token

			return 0;
		}
	}
}