using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using IOCModelBinderExample.Contracts;
using IOCModelBinderExample.ViewModels;
using IOCModelBinderExample.Domain;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using IOCModelBinderExample.Infrastructure;

namespace IOCModelBinderExample
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
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc().AddMvcOptions(options =>
            {
                IModelBinderProvider originalProvider = options.ModelBinderProviders.FirstOrDefault(x => x.GetType() == typeof(ComplexTypeModelBinderProvider));
                int originalBinderIndex = options.ModelBinderProviders.IndexOf(originalProvider);
                options.ModelBinderProviders.Remove(originalProvider);
                options.ModelBinderProviders.Insert(originalBinderIndex, new IocModelBinderProvider());
            });

            // My type registrations (typically autoregisted in Autofac)
            services.AddTransient<ICustomerRepository, CustomerRepository>();
            services.AddTransient<ICustomer, Customer>();

            // ViewModels (typically autoregisted in Autofac)
            services.AddTransient<HomeViewModel, HomeViewModel>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
