using CA2_Assignment.Services;
using CA2_Web.Configurations;
using CA2_Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CA2_Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            #region ConfigureServices - Configure DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            #endregion
            #region ConfigureServices - Configure Identity Server
            services.AddDefaultIdentity<IdentityUser>(config =>
            {
                //config.SignIn.RequireConfirmedEmail = true;
            })
                .AddDefaultUI(UIFramework.Bootstrap4)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 5;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            });
            #endregion
            #region ConfigureServices - Configure Version & Security
            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {

                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            #endregion
            #region ConfigureServices - Configure Cookie Policies
            services.AddSession();
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            #endregion
            #region ConfigureServices - Enforce HTTPS
            services.Configure<MvcOptions>(options =>
            {
                options.Filters.Add(new RequireHttpsAttribute());
            });
            #endregion

            #region ConfigureServices - Inject Settings
            services.Configure<AppConfigurations>(Configuration.GetSection("AppConfigurations"));

            services.Configure<SendGridConfigurations>(Configuration.GetSection("SendGridConfigurations"));
            services.Configure<GoogleAuthConfigurations>(Configuration.GetSection("GoogleAuthConfigurations"));
            services.Configure<CaptchaConfigurations>(Configuration.GetSection("CaptchaConfigurations"));

            services.Configure<AwsConfigurations>(Configuration.GetSection("AwsConfigurations"));
            services.Configure<AwsS3Configurations>(Configuration.GetSection("AwsS3Configurations"));
            services.Configure<AwsDynamoConfigurations>(Configuration.GetSection("AwsDynamoConfigurations"));
            #endregion
            #region ConfigureServices - Inject Dependencies
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IAuthorizationHandler, AccessLevelHandler>();

            services.AddSingleton<IAwsService, AwsService>();
            services.AddSingleton<IEmailSender, EmailService>();
            services.AddSingleton<ICaptchaService, CaptchaService>();
            #endregion

            #region ConfigureServices - Define Authorization Policies
            services.AddAuthorization(options =>
            {
                options.AddPolicy(
                    "AccessLevel01",
                    policy => policy.Requirements.Add(new AccessLevelRequirement(1)));
                options.AddPolicy(
                    "AccessLevel02",
                    policy => policy.Requirements.Add(new AccessLevelRequirement(2)));
                options.AddPolicy(
                    "AccessLevel03",
                    policy => policy.Requirements.Add(new AccessLevelRequirement(3)));
            });
            #endregion

            #region ConfigureServices - Configure Google Authentication
            services.AddAuthentication().AddGoogle(googleOptions =>
            {
                googleOptions.ClientId = Configuration.GetSection("GoogleAuthConfigurations")["ClientId"];
                googleOptions.ClientSecret = Configuration.GetSection("GoogleAuthConfigurations")["ClientKey"];
            });
            #endregion
        }
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            #region Configure - Define Settings
            app.UseHttpsRedirection();

            app.UseCookiePolicy();
            app.UseSession();

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc();
            #endregion
        }
    }
}