using ChurchWebApi.Controllers;
using ChurchWebApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System.IO;

namespace ChurchWebApi
{
    public class Startup
    {
        private ILogger<Startup> _logger;

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IChurchService, ChurchService>();
            services.AddSingleton<IDatabaseConnector, DatabaseConnector>();
            services.AddSingleton<ISecureKeyRetriever, SecureKeyRetriever>();
            services.AddSingleton<IEncryptionLayer, EncryptionLayer>();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Place Info Service API",
                    Version = "v1",
                    Description = "Sample service for Learner",
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime, ILoggerFactory loggerFactory, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var logFilePath = Configuration.GetSection("LogFilePath").Value ?? Directory.GetCurrentDirectory();
            log4net.GlobalContext.Properties["LogFilePath"] = logFilePath;

            loggerFactory.AddLog4Net();
            _logger = logger;

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            { 
                endpoints.MapControllers(); 
            });

            // Enable middleware to serve generated Swagger as a JSON endpoint.  
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),  
            // specifying the Swagger JSON endpoint.  
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Church Services"));

            applicationLifetime.ApplicationStarted.Register(OnStartup);
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
        }

        private void OnStartup()
        {
            _logger.LogInformation("The Church API service has started.");
        }

        private void OnShutdown()
        {
            _logger.LogInformation("The Church API service is stopping.");
        }
    }
}
