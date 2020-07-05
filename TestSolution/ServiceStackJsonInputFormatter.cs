using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tavisca.Platform.Common.Profiling;

namespace TestSolution
{
    public class ServiceStackJsonInputFormatter : InputFormatter
    {
        public ServiceStackJsonInputFormatter()
        {
            SupportedMediaTypes.Add(new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/ecmascript"));
        }

        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using (new TraceProfileContext("input_formatter", "ServiceStack Json Input Formatter - ReadRequestBodyAsync"))
            {
                using (var streamReader = new StreamReader(context.HttpContext.Request.Body, Encoding.UTF8, true, 1024, true))
                {
                    var obj = ServiceStack.Text.JsonSerializer.DeserializeFromReader(streamReader, context.ModelType);

                    return Task.FromResult(InputFormatterResult.Success(obj));
                }
            }
        }
    }

}
