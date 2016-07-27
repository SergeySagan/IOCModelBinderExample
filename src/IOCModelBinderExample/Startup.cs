using IOCModelBinderExample.Contracts;
using IOCModelBinderExample.Domain;
using IOCModelBinderExample.Infrastructure;
using IOCModelBinderExample.ViewModels;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace IOCModelBinderExample
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            if (env.IsDevelopment())
            {
                // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                builder.AddApplicationInsightsSettings(developerMode: true);
            }
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseApplicationInsightsRequestTelemetry();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseIISPlatformHandler();

            app.UseApplicationInsightsExceptionTelemetry();

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc().AddMvcOptions(options =>
            {
                //options.ModelBinders.Insert(0, new IocModelBinder());
                IModelBinder originalBinder = options.ModelBinders.FirstOrDefault(x => x.GetType() == typeof(MutableObjectModelBinder));
                int originalBinderIndex = options.ModelBinders.IndexOf(originalBinder);
                options.ModelBinders.Remove(originalBinder);
                options.ModelBinders.Insert(originalBinderIndex, new IocModelBinder(options.ModelBinders));
            });

            // My type registrations (typically autoregisted in Autofac)
            services.AddTransient<ICustomerRepository, CustomerRepository>();
            services.AddTransient<ICustomer, Customer>();

            // ViewModels (typically autoregisted in Autofac)
            services.AddTransient<HomeViewModel, HomeViewModel>();
        }
    }
}