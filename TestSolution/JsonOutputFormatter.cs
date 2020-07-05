using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tavisca.Platform.Common.Profiling;

namespace TestSolution
{
    public class JsonOutputFormatter : OutputFormatter
    {
        public JsonOutputFormatter(Microsoft.AspNetCore.Mvc.Formatters.JsonOutputFormatter inner)
        {
            _inner = inner;
        }

        Microsoft.AspNetCore.Mvc.Formatters.JsonOutputFormatter _inner;

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            using (new ProfileContext("Json Output Formatter - WriteResponseBodyAsync"))
            {
                await _inner.WriteResponseBodyAsync(context);
            }
        }

        public override bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return _inner.CanWriteResult(context);
        }

        public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        {
            return _inner.GetSupportedContentTypes(contentType, objectType);
        }

        public override Task WriteAsync(OutputFormatterWriteContext context)
        {
            using (new TraceProfileContext("output_formatter", "Json Output Formatter - WriteAsync"))
            {
                return _inner.WriteAsync(context);
            }
        }

        public override void WriteResponseHeaders(OutputFormatterWriteContext context)
        {
            using (new ProfileContext("Json Output Formatter - WriteResponseHeaders"))
            {
                _inner.WriteResponseHeaders(context);
            }
        }
    }
}
