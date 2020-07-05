using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Tavisca.Common.Plugins.Configuration;
using Tavisca.Common.Plugins.StructureMap;
using Tavisca.Platform.Common;
using Tavisca.Platform.Common.ApplicationEventBus;
using Tavisca.Platform.Common.Configurations;
using Tavisca.Platform.Common.Containers;
using Tavisca.Platform.Common.Core.ServiceLocator;
using Tavisca.Platform.Common.ExceptionManagement;
using Tavisca.Platform.Common.Logging;
using Tavisca.Platform.Common.MemoryStreamPool;
using Tavisca.Platform.Common.Serialization;

namespace TestSolution
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
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

            services.AddSingleton<IConfiguration>(Configuration);
            var serviceLocator = new NetCoreServiceLocator(new ContainerFactory(services), GetModules());

            ServiceLocator.SetLocatorProvider(() => serviceLocator);
            var configurationProvider = ServiceLocator.Current.GetInstance<Tavisca.Libraries.Configuration.IConfigurationProvider>();

            services.AddResponseCompression(opt =>
            {
                opt.Providers.Add<GzipCompressionProvider>();
                opt.EnableForHttps = true;
            });

            var gzipCompressionMiddlewareSettings = configurationProvider.GetGlobalConfiguration<GzipCompressionMiddlewareSettings>(Model.Models.Common.KeyStore.Consul.ConfigurationSections.ApplicationSettings, Model.Models.Common.KeyStore.Consul.ApplicationSettings.GzipCompressionMiddlewareSettings);
            _isCompressionEnabled = gzipCompressionMiddlewareSettings?.IsEnabled ?? false;
            if (_isCompressionEnabled)  
            {
                var compressionLevel = GetCompressionLevel(gzipCompressionMiddlewareSettings);
                services.Configure<GzipCompressionProviderOptions>(options => options.Level = compressionLevel);
            }

            SetLoggingThreadPool(configurationProvider);
            ServicePointManagerSettings();

            Logger.Initialize(ServiceLocator.Current.GetInstance<ILogWriterFactory>());

            InitializeServices();

            var serviceProvider = ServiceLocator.Current.GetInstance<IServiceProvider>();
            //register config change event handler in application bus
            var bus = serviceProvider.GetRequiredService<IApplicationEventBus>();
            InitializeApplicationServiceBus(bus);
            ExceptionPolicy.Configure(ServiceLocator.Current.GetInstance<IErrorHandler>());

            return serviceProvider;
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

        private IModule[] GetModules()
        {
            return new IModule[]{
                new Module(),
                //new Core.Module(),
                //new Service.Module()
            };
        }

        private CompressionLevel GetCompressionLevel(GzipCompressionMiddlewareSettings gzipCompressionMiddlewareSettings)
        {
            if (gzipCompressionMiddlewareSettings == null)
                return CompressionLevel.Fastest;

            switch (gzipCompressionMiddlewareSettings.CompressionLevel.ToLower())
            {
                case "fastest": return CompressionLevel.Fastest;
                case "optimal": return CompressionLevel.Optimal;
                case "nocompression": return CompressionLevel.NoCompression;
                default: return CompressionLevel.Fastest;
            }
        }

        private static void SetLoggingThreadPool(Platform.Common.Configurations.IConfigurationProvider configurationProvider)
        {
            AsyncTasks.UseRoundRobinPool();

            var loggingThreadPoolSize = configurationProvider.GetGlobalConfiguration<int>(Model.Models.Common.KeyStore.Consul.ConfigurationSections.Logging, Model.Models.Common.KeyStore.Logging.ThreadPoolSize);
            loggingThreadPoolSize = (loggingThreadPoolSize == 0) ? Model.Models.Common.KeyStore.Defaults.LoggingThreadPoolSize : loggingThreadPoolSize;
            AsyncTasks.AddPool("logging", loggingThreadPoolSize);
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
