using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using ControlPoint.Api;
using ControlPoint.Api.Controllers;
//using log4net;

namespace Busidex.Api.HttpPipeline
{
	/// <summary>
	/// Indicates that a controller method may be invoked only if the special X-Unity-App-Name header is present.
	/// This header is always stripped from external requests, so only apps within the datacenter may invoke it.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class InternalUseOnlyAttribute : ActionFilterAttribute
	{
		private const string APP_ACCESS_HEADER = "X-Unity-App-Name";
		//private static readonly ILog s_log = LogManager.GetLogger(typeof(InternalUseOnlyAttribute));

		public InternalUseOnlyAttribute()
		{
		}

		public override void OnActionExecuting(HttpActionContext context)
		{
			string appName = GetSecuredAppNameAccessHeader(context.Request);
			context.Request.Properties.Add(BaseApiController.REQ_PROP_CLIENT_APP_NAME, appName);
		}

		/// <summary>
		/// Gets the name of the app making a secured request.  For sensitive API methods (like cash transactions),
		/// this ensures that a request comes from an internal, authorized application rather than an external API client.
		/// </summary>
		/// <returns>The app name.  Guaranteed to be a non-empty string.</returns>
		/// <exception cref="ApiException">The header is missing or invalid.</exception>
		protected string GetSecuredAppNameAccessHeader(HttpRequestMessage request)
		{
			string firstAppName = null;

			IEnumerable<string> appNames;
			if (request.Headers.TryGetValues(APP_ACCESS_HEADER, out appNames))
			{
				foreach (string appName in appNames)
				{
					if (string.IsNullOrEmpty(appName))
					{
						continue;
					}

					if (firstAppName != null)
					{
						throw new ApiException(HttpStatusCode.BadRequest, "Multiple " + APP_ACCESS_HEADER + " headers present");
					}
					firstAppName = appName;
				}

				if (firstAppName != null)
				{
					return firstAppName;
				}
			}

			//s_log.Warn("External access attempted on secure method");
			throw new ApiException(HttpStatusCode.Forbidden, "Internal use only");
		}
	}
}