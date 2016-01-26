using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Collections.Specialized;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using ControlPoint.Api.Models;
using NUnit.Framework;
using RestSharp;
using ControlPoint.Api.Tests;

namespace ControlPoint.Api.Tests
{
	/// <summary>
	/// Creates an API client that can issue HTTP requests and read responses.
	/// Includes built-in support for OAuth 2.0 authentication and API exception handling (throwing ApiExceptions where appropriate).
	/// </summary>
	public class ApiClient : IDisposable
	{
		private readonly string _baseUrl;
		private readonly HttpServer _server;
		private readonly HttpClient _client;
		private bool _disposed;
		private string _accessToken;
		private string _refreshToken;
		private string _acceptContentType = "application/json";
		private readonly IDictionary<string, string> _requestHeaders = new Dictionary<string, string>();

		static ApiClient()
		{
			// Trust all certificates
			ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);
		}

		/// <summary>
		/// Creates an API client that communicates with a self-managed in-memory API server.
		/// </summary>
		public ApiClient()
			: this(null)
		{
		}

		/// <summary>
		/// Creates an API client communicating with a server at the given base URL.
		/// If the URL is blank, an in-memory server is instantiated instead.
		/// </summary>
		public ApiClient(Uri apiUrl)
		{
			if (apiUrl == null)
			{
				_server = ApiServerFactory.CreateInMemoryApiServer();
				_client = new HttpClient(_server);
				_baseUrl = "http://localhost";
			}
			else
			{
				_server = null;
				_client = new HttpClient();
				_baseUrl = apiUrl.ToString();
				if (_baseUrl.EndsWith("/"))
				{
					_baseUrl = _baseUrl.Substring(0, _baseUrl.Length - 1);
				}
			}
		}

		public string AcceptContentType
		{
			get { return _acceptContentType; }
			set { _acceptContentType = value; }
		}

		public IDictionary<string, string> RequestHeaders
		{
			get { return _requestHeaders; }
		}

		public virtual void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			if (_client != null)
			{
				if (_refreshToken != null)
				{
					try
					{
						RevokeToken(_refreshToken);
					}
					catch
					{
						// Ignore revoke errors on dispose
					}
				}
				_client.Dispose();
			}
			if (_server != null)
			{
				_server.Dispose();
			}

			_disposed = true;
		}

		public virtual OAuthTokenResponse AcquireAuthToken(string username, string password, string scope = null, string grantType = null)
		{
			//OAuthTokenResponse resp = ExecuteRequest<OAuthTokenResponse>(HttpMethod.Post, "/authentication",
			//															 new Dictionary<string, string>
			//																 {
			//																	 {"username", username},
			//																	 {"password", password},
			//																	 {"grant_type", grantType},
			//																	 {"scope", scope},
			//																 });
			OAuthTokenResponse resp = ExecuteRequest<OAuthTokenResponse>(HttpMethod.Post, "/authentication",
																		 null, 
																		 new LoginParams
																		 {
																			 UserName = username,
																			 Password = password
																		 });
			_accessToken = resp.access_token;
			_refreshToken = resp.refresh_token;
			return resp;
		}

		public virtual OAuthRefreshTokenResponse RefreshAuthToken(string scope = null, string grantType = "refresh_token", string refreshToken = null)
		{
			OAuthRefreshTokenResponse resp = ExecuteRequest<OAuthRefreshTokenResponse>(HttpMethod.Post, "/oauth2/token",
																					   new Dictionary<string, string>
																						   {
																							   {"refresh_token", refreshToken ?? _refreshToken},
																							   {"grant_type", grantType},
																							   {"scope", scope},
																						   });
			_accessToken = resp.access_token;
			return resp;
		}

		public virtual void RevokeToken(string refreshToken = null)
		{
			ExecuteRequest(HttpMethod.Post, "/oauth2/revoke",
						   new Dictionary<string, string>
							   {
								   {"refresh_token", refreshToken ?? _refreshToken}
							   });
			_accessToken = null;
			_refreshToken = null;
		}

		public virtual T ExecuteEntityRequest<T>(HttpMethod method, string path, T bodyContent)
		{
			return ExecuteRequest<T>(method, path, null, bodyContent);
		}

		public virtual T ExecuteRequest<T>(HttpMethod method, string path, IEnumerable<KeyValuePair<string, string>> parameters = null, object bodyContent = null)
		{
			using (HttpRequestMessage request = CreateRequest(method, path, parameters, bodyContent))
			{
				using (HttpResponseMessage response = _client.SendAsync(request).Result)
				{
					return ParseResponse<T>(response);
				}
			}
		}

		public virtual void ExecuteRequest(HttpMethod method, string path, IEnumerable<KeyValuePair<string, string>> parameters = null, object bodyContent = null)
		{
			using (HttpRequestMessage request = CreateRequest(method, path, parameters, bodyContent))
			{
				using (HttpResponseMessage response = _client.SendAsync(request).Result)
				{
					ParseResponse<byte[]>(response);
				}
			}
		}

		protected virtual HttpRequestMessage CreateRequest(HttpMethod method, string path, IEnumerable<KeyValuePair<string, string>> parameters, object payloadEntity)
		{
			UriBuilder uriBuilder = new UriBuilder(_baseUrl + path);
			HttpContent payloadContent = null;

			// Add payload content
			if (payloadEntity != null)
			{
				if (payloadEntity is HttpContent)
				{
					payloadContent = (HttpContent)payloadEntity;
				}
				else
				{
					//payloadContent = new StringContent(SimpleJson.SerializeObject(payloadEntity), new UTF8Encoding(), "application/json");
					payloadContent = new JsonContent(payloadEntity);
				}
			}

			// Add parameters
			if (parameters != null)
			{
				if (payloadContent != null || method == HttpMethod.Get || method == HttpMethod.Delete)
				{
					NameValueCollection qs = HttpUtility.ParseQueryString(string.Empty);
					foreach (KeyValuePair<string, string> entry in parameters)
					{
						qs.Add(entry.Key, entry.Value);
					}
					uriBuilder.Query = qs.ToString();
				}
				else
				{
					payloadContent = new FormUrlEncodedContent(parameters);
				}
			}

			// Build request (method + URL + pararameters + payload)
			var request = new HttpRequestMessage
			{
				Method = method,
				RequestUri = uriBuilder.Uri,
				Content = payloadContent
			};

			// Add "Accept" header so we can dictate the response type explicitly
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(AcceptContentType));

			// Add OAuth 2.0 authorization header (if we have a valid access token)
			if (_accessToken != null)
			{
				request.Headers.Add("Authorization", string.Format("Bearer {0}", _accessToken));
			}

			// Add custom headers
			foreach (KeyValuePair<string, string> entry in RequestHeaders)
			{
				request.Headers.Add(entry.Key, entry.Value);
			}

			return request;
		}

		protected virtual T ParseResponse<T>(HttpResponseMessage response)
		{
			if (response.StatusCode == HttpStatusCode.NoContent)
			{
				// No content -- return default value
				Assert.IsTrue(response.Content == null || string.IsNullOrEmpty(response.Content.ReadAsStringAsync().Result));
				return default(T);
			}

			// Anything with code 300+ is an error to be handled by raising an API exception
			if ((int)response.StatusCode >= 300)
			{
				HandleErrorResponse(response);
			}

			// Verify MIME type
			Assert.AreEqual(AcceptContentType, response.Content.Headers.ContentType.MediaType);

			if (typeof(T) == typeof(byte[]))
			{
				// Simply return byte array
				return (T)(object)response.Content.ReadAsByteArrayAsync().Result;
			}

			// Deserialize response
			return response.Content.ReadAsAsync<T>().Result;
		}

		protected virtual void HandleErrorResponse(HttpResponseMessage response)
		{
			ErrorException error;
			try
			{
				error = response.Content.ReadAsAsync<ErrorException>().Result;
			}
			catch (Exception ex)
			{
				error = new ErrorException
				{
					Message = ex.Message,
					StackTrace = ex.StackTrace
				};
			}
			throw new ApiException(response.StatusCode, (error == null) ? null : error.Message);
		}
	}
}