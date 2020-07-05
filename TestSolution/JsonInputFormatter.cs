using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Tavisca.Platform.Common.Profiling;
namespace TestSolution
{
    public class JsonInputFormatter : InputFormatter
    {
        public JsonInputFormatter(Microsoft.AspNetCore.Mvc.Formatters.JsonInputFormatter inner)
        {
            _inner = inner;
        }

        Microsoft.AspNetCore.Mvc.Formatters.JsonInputFormatter _inner;

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            using (new ProfileContext("Json Input Formatter - ReadRequestBodyAsync"))
            {
                return await _inner.ReadRequestBodyAsync(context);
            }
        }

        public override bool CanRead(InputFormatterContext context)
        {
            return _inner.CanRead(context);
        }

        public override IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        {
            return _inner.GetSupportedContentTypes(contentType, objectType);
        }

        public override Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            using (new ProfileContext("input_formatter - Json Input Formatter - ReadAsync"))
            {
                return _inner.ReadAsync(context);
            }
        }
    }
}
