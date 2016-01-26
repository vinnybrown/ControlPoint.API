using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ControlPoint.Api.Models
{
	public class JsonContent : HttpContent
	{

		private readonly MemoryStream _Stream = new MemoryStream();
		public JsonContent(object value)
		{

			var jw = new JsonTextWriter(new StreamWriter(_Stream));
			jw.Formatting = Formatting.Indented;
			var serializer = new JsonSerializer();
			serializer.Serialize(jw, value);
			jw.Flush();
			_Stream.Position = 0;

			Headers.ContentType = new MediaTypeHeaderValue("application/json");
		}
		protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
		{
			return _Stream.CopyToAsync(stream);
		}

		protected override bool TryComputeLength(out long length)
		{
			length = _Stream.Length;
			return true;
		}
	}
}