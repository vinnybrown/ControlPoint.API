using System;
using System.Configuration;
using NUnit.Framework;

namespace ControlPoint.Api.Tests.Controllers
{
	public abstract class BaseControllerTest : IDisposable
	{
		private ApiClient _client;

		protected BaseControllerTest()
		{
		}

		[TestFixtureSetUp]
		public virtual void Setup()
		{
			CreateApiClient();
		}

		[TestFixtureTearDown]
		public virtual void Dispose()
		{
			if (_client != null)
			{
				_client.Dispose();
				_client = null;
			}
		}

		protected virtual ApiClient ApiClient
		{
			get { return _client; }
		}

		protected virtual void CreateApiClient()
		{
			string url = ConfigurationManager.AppSettings["ApiUrl"];
			_client = new ApiClient(string.IsNullOrEmpty(url) ? null : new Uri(url));
			AutoAuthenticate(_client);
		}

		protected virtual void AutoAuthenticate(ApiClient client)
		{
			client.AcquireAuthToken(ConfigurationManager.AppSettings["username"], ConfigurationManager.AppSettings["password"]);
		}
	}
}