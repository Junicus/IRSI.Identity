using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IRSI.Identity.Data;
using IRSI.Identity.Models;
using IRSI.Identity.Services;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using IdentityServer4.EntityFramework.DbContexts;
using IRSI.Identity.IdentityServer;
using IdentityServer4.EntityFramework.Mappers;

namespace IRSI.Identity
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            Environment = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment Environment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certCollection = certStore.Certificates.Find(
                X509FindType.FindByThumbprint,
                Configuration["IdentityServer:Thumbprint"],
                false);
            X509Certificate2 cert = null;

            if (certCollection.Count > 0)
            {
                cert = certCollection[0];
            }

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("IdentityServer")));

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddRoleManager<ApplicationRoleManager>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddIdentityServerUserClaimsPrincipalFactory();

            if (Environment.IsDevelopment() || cert == null)
            {
                services.AddIdentityServer()
                    .AddTemporarySigningCredential()
                    .AddConfigurationStore(builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("IdentityServer"), options =>
                            options.MigrationsAssembly(migrationAssembly)))
                    .AddOperationalStore(builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("IdentityServer"), options =>
                            options.MigrationsAssembly(migrationAssembly)))
                    .AddAspNetIdentity<ApplicationUser>();

            }
            else
            {
                services.AddIdentityServer()
                    .AddSigningCredential(cert)
                    .AddConfigurationStore(builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("IdentityServer"), options =>
                            options.MigrationsAssembly(migrationAssembly)))
                    .AddOperationalStore(builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("IdentityServer"), options =>
                            options.MigrationsAssembly(migrationAssembly)))
                    .AddAspNetIdentity<ApplicationUser>();
            }

            services.AddMvc();

            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            InitializeDatabase(app);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseWhen(context => context.Request.Path.StartsWithSegments(new PathString("/api")), branch =>
            {
                //setup api bearer tokens against IdentityServer4
                app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
                {
                    Authority = "http://localhost:52000",
                    RequireHttpsMetadata = false,
                    ApiName = "idManage"
                });
            });

            app.UseWhen(context => !context.Request.Path.StartsWithSegments(new PathString("/api")), branch =>
            {
                //Setup authentication for non api calls
                app.UseIdentity();
                app.UseIdentityServer();

                var externalCoockieScheme = app.ApplicationServices.GetRequiredService<IOptions<IdentityOptions>>().Value.Cookies.ExternalCookieAuthenticationScheme;
                app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
                {
                    DisplayName = "IRSI Email",
                    ClientId = Configuration["Authentication:AzureAd:ClientId"],
                    ClientSecret = Configuration["Authentication:AzureAd:ClientSecret"],
                    Authority = Configuration["Authentication:AzureAd:AADInstance"] + Configuration["Authentication:AzureAd:TenantId"],
                    CallbackPath = Configuration["Authentication:AzureAd:CallbackPath"],
                    ResponseType = OpenIdConnectResponseType.IdToken,
                    AutomaticChallenge = false,
                    TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    }
                });
            });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void InitializeDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                if (!context.Clients.Any())
                {
                    foreach (var client in Config.GetClients())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var idResource in Config.GetIdentityResources())
                    {
                        context.IdentityResources.Add(idResource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var apiResource in Config.GetApiResources())
                    {
                        context.ApiResources.Add(apiResource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
