using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Tavisca.Platform.Common.Profiling;

namespace TestSolution
{
    public class NewtonsoftJsonInputFormatter : InputFormatter
    {
        public NewtonsoftJsonInputFormatter()
        {
            _jsonSerializer = new Newtonsoft.Json.JsonSerializer()
            {
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            _jsonSerializer.Converters.Add(new StringEnumConverter());
            SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/javascript"));
        }

        private readonly Newtonsoft.Json.JsonSerializer _jsonSerializer;

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using (new ProfileContext("input_formatter - Newtonsoft Json Input Formatter - ReadRequestBodyAsync"))
            {
                using (var streamReader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    var obj = _jsonSerializer.Deserialize(streamReader, context.ModelType);

                    return Task.FromResult(InputFormatterResult.Success(obj));
                }
            }
        }
    }
}
