using Newtonsoft.Json;

namespace ControlPoint.Api.Models
{
	public class OAuthRefreshTokenResponse : OAuthTokenResponse
	{
		[JsonIgnore]
		public new string refresh_token { get; set; }
	}
}