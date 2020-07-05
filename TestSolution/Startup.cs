using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TestSolution
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                            .AddEnvironmentVariables();


            Configuration = builder.Build();
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", env.EnvironmentName);
            _environmentName = env.EnvironmentName;
            Console.WriteLine("Processor count for Web Application: {0}", Environment.ProcessorCount);
        }

        private readonly string _environmentName;
        private bool _isCompressionEnabled;
        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ThreadPool.SetMinThreads(200, 200);

            AddMvcCoreAndItsExtension(services);
        }

        private static void AddMvcCoreAndItsExtension(IServiceCollection services)
        {
            services.AddMvcCore()
            .AddJsonFormatters()
            .AddMvcOptions(mvcOptions =>
            {
                var jsonInputFormatter = (Microsoft.AspNetCore.Mvc.Formatters.JsonInputFormatter)mvcOptions.InputFormatters[1];
                var profileInjectedInputFormatter = new JsonInputFormatter(jsonInputFormatter);
                mvcOptions.InputFormatters.Clear();
                mvcOptions.InputFormatters.Add(profileInjectedInputFormatter);

                mvcOptions.InputFormatters.Add(new NewtonsoftJsonInputFormatter());
                mvcOptions.InputFormatters.Add(new ServiceStackJsonInputFormatter());

                var jsonOutputFormatter = (Microsoft.AspNetCore.Mvc.Formatters.JsonOutputFormatter)mvcOptions.OutputFormatters[3];
                var profileInjectedOutputFormatter = new JsonOutputFormatter(jsonOutputFormatter);
                mvcOptions.OutputFormatters.RemoveAt(3);
                mvcOptions.OutputFormatters.Add(profileInjectedOutputFormatter);
            })
            .AddJsonOptions(options =>
            {
                options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                options.SerializerSettings.Converters.Add(new StringEnumConverter());

            })
            .AddCors();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}
