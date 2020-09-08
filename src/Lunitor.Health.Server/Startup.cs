using Lunitor.Health.Server.BackgroundCheck;
using Lunitor.Health.Server.Notification;
using Lunitor.Health.Server.Notification.Configurations;
using Lunitor.Health.Server.Service;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lunitor.Health.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IServiceStore, ServiceStore>();
            services.AddHttpClient<IServiceChecker, ServiceChecker>();

            services.Configure<SmtpConfiguration>(Configuration.GetSection("SmtpConfiguration"));
            services.Configure<NotificationConfiguration>(Configuration.GetSection("NotificationConfiguration"));
            services.AddSingleton<IEmailBuilder, EmailBuilder>();
            services.AddTransient<ISmtpClient, SmtpClient>();
            services.AddTransient<INotificationService, EmailNotificationService>();

            services.Configure<BackgroundCheckerConfiguration>(Configuration.GetSection("BackgroundCheckerConfiguration"));
            services.AddHostedService<BackgroundChecker>();

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
