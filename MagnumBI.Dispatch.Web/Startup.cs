// 
// 0918
// 2017091812:37 PM

using MagnumBI.Dispatch.Web.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagnumBI.Dispatch.Web {
    /// <summary>
    ///     The startup class for MagnumBI Dispatch
    /// </summary>
    public class Startup {
        /// <summary>
        ///     The configuration object for ASP.Net.
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// </summary>
        /// <param name="env"></param>
        public Startup(IHostingEnvironment env) {
            IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

            builder.AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to add services to the container
        /// </summary>
        /// <param name="services">Services to add to the container</param>
        public void ConfigureServices(IServiceCollection services) {
            services.Configure<RouteOptions>(options => {
                options.AppendTrailingSlash = true;
                options.LowercaseUrls = true;
            });

            // Add framework services.
            services.AddMvc();
        }

        /// <summary>
        ///     This is standard boilerplate ASP.Net.
        ///     Provides configuration for the ASP.Net framework.
        /// </summary>
        public void Configure(IApplicationBuilder app,
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IApplicationLifetime applicationLifetime) {
            ILogger logger = loggerFactory.CreateLogger("Main");
            // Application configuration

            app.UseMiddleware<AuthenticationMiddleware>();

            app.UseMvc();

            applicationLifetime.ApplicationStopping.Register(this.OnShutdown);
        }

        private void OnShutdown() {
            // stop running threads
            EngineHealthChecker.Stop();
            JobMaintenanceHelper.Stop();
        }
    }
}