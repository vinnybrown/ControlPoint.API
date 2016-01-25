using System;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Configuration;
using System.Web.Http;

namespace ControlPoint.Api.Controllers
{
    public class BaseApiController : ApiController
    {
		internal static readonly string REQ_PROP_CLIENT_APP_NAME = "BaseApiCbntroller.AppName";
		internal static readonly string REQ_PROP_USER_ID = "BaseApiCbntroller.UserID";

		protected long ValidateUser()
		{
			long userId = 0;
			try
			{
				var header = Request.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant().Equals("x-authorization-token"));
				var val = header.Value != null ? header.Value.FirstOrDefault() : string.Empty;

				if (!string.IsNullOrEmpty(val))
				{
					var userIdStr = Encoding.ASCII.GetString(Convert.FromBase64String(val));
					long.TryParse(userIdStr, out userId);
				}
			}
			catch (Exception ex)
			{
				// save application error
			}
			return userId;
		}

		protected string EncodeString(string newString)
		{
			Configuration cfg =
				WebConfigurationManager.OpenWebConfiguration(System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
			var machineKey = (MachineKeySection)cfg.GetSection("system.web/machineKey");
			var hash = new HMACSHA1 { Key = HexToByte(machineKey.ValidationKey) };
			var encodedString = Convert.ToBase64String(hash.ComputeHash(Encoding.Unicode.GetBytes(newString)));
			return encodedString;
		}

		protected byte[] HexToByte(string hexString)
		{
			var returnBytes = new byte[hexString.Length / 2];
			for (int i = 0; i < returnBytes.Length; i++)
				returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
			return returnBytes;
		}

		protected static byte[] GetBytes(string str)
		{
			var bytes = new byte[str.Length * sizeof(char)];
			Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
			return bytes;
		}

		protected static string GetString(byte[] bytes)
		{
			var chars = new char[bytes.Length / sizeof(char)];
			Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
			return new string(chars);
		}
	}
}
