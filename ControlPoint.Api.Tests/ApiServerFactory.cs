using System.Web.Http;

namespace ControlPoint.Api.Tests
{
	public static class ApiServerFactory
	{
		/// <summary>
		/// Instantiates an HttpServer that deploys Api.Core in-memory.  No HTTP port is actually opened.
		/// </summary>
		public static HttpServer CreateInMemoryApiServer()
		{
			var config = new HttpConfiguration();
			AttributeRoutingHttpConfig.Configure(config, true);
			return new HttpServer(config);
		}
	}
}