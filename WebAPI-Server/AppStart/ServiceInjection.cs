﻿using System;
using System.Text;
using Common;
using Dapper.Identity;
using Dapper.Identity.Stores;
using ElmahCore.Mvc;
using ElmahCore.Sql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WebAPI_BAL.IdentityManager;
using WebAPI_BAL.JwtGenerator;
using WebAPI_Server.Middleware;
using WebAPI_ViewModel.ConfigSettings;

namespace WebAPI_Server.AppStart
{
    internal static partial class ServiceInjection
    {
        internal static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {

            //string connString = configuration.GetConnectionString("DefaultConnection");
            //string connString = configuration.GetConnectionString("TicketSystem");
            string greeterAppConnString = configuration.GetConnectionString("GreeterApp");

            services.Configure<ApplicationDbOptions>(options =>
            {
                options.LogQuery = true;
                options.ConnectionString = greeterAppConnString;
                options.SqlProvider = SqlProvider.MSSQL;
                options.UseQuotationMarks = true;
            });

        }

        internal static void ConfigureJwt(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
            // configure strongly typed settings objects
            var appSettingsSection = configuration.GetSection(nameof(AppSettings));
            var jwtAppSettingOptions = configuration.GetSection(nameof(JwtIssuerOptions));
            var jwtChatSettingOptions = configuration.GetSection(nameof(JwtChatIssuerOptions));

            services.Configure<AppSettings>(appSettingsSection);
            var appSettings = appSettingsSection.Get<AppSettings>();

            // configure jwt authentication
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.SecretKey));

            // Configure JwtIssuerOptions
            services.Configure<JwtIssuerOptions>(options =>
            {
                options.ValidFor = TimeSpan.Parse(jwtAppSettingOptions[nameof(JwtIssuerOptions.ValidFor)]);
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512);
            });

            // Configure JwtChatIssuerOptions
            services.Configure<JwtChatIssuerOptions>(options =>
            {
                options.ValidFor = TimeSpan.Parse(jwtChatSettingOptions[nameof(JwtChatIssuerOptions.ValidFor)]);
                options.Issuer = jwtChatSettingOptions[nameof(JwtChatIssuerOptions.Issuer)];
                options.Audience = jwtChatSettingOptions[nameof(JwtChatIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha512);
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)],

                ValidateAudience = true,
                ValidAudience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)],

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                RequireSignedTokens = true,

                RequireExpirationTime = true,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(0)
            };

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(x =>
                {
                    x.ClaimsIssuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = tokenValidationParameters;
                });

            //// api user claim policy
            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("ApiUser", policy => policy.RequireClaim(Constants.Strings.JwtClaimIdentifiers.Rol, Constants.Strings.JwtClaims.ApiAccess));
            //});

        }

        internal static void ConfigureDependencyServices(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");

            if (configuration.GetSection(nameof(FacebookAuthSettings)).Value != null)
                services.Configure<FacebookAuthSettings>(configuration.GetSection(nameof(FacebookAuthSettings)));

            if (configuration.GetSection(nameof(SmtpConfig)).Value != null)
                services.Configure<SmtpConfig>(configuration.GetSection(nameof(SmtpConfig)));

            SingletonServices(services);

            services.AddScoped<IPasswordHasher<ApplicationUser>, WebAPI_BAL.IdentityManager.PasswordHasher<ApplicationUser>>();

            services.AddTransient<TokenManagerMiddleware>();
            //services.AddTransient<AngularAntiforgeryCookieResultFilter>();
            services.AddTransient<ITokenManager, TokenManager>();
            //services.AddDistributedRedisCache(r => { r.Configuration = configuration["redis:connectionString"]; });
            services.AddDistributedRedisCache(r => {
                r.ConfigurationOptions = new StackExchange.Redis.ConfigurationOptions()
                {
                    EndPoints = { configuration["redis:server"] },
                    Password = configuration["redis:password"],
                    DefaultDatabase = Convert.ToInt32(configuration["redis:db"]),
                    AllowAdmin = true,
                    ConnectTimeout = 5000,
                    SyncTimeout = 5000,
                };
            });

            services.AddScoped<UserStore>();
            services.AddScoped<RoleStore>();
            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, AppClaimsPrincipalFactory>();

            services.AddIdentity<ApplicationUser, ApplicationRole>(
                    options =>
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequireLowercase = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequiredLength = 8;
                        options.Password.RequireNonAlphanumeric = true;

                        options.SignIn.RequireConfirmedEmail = true;
                    })
                .AddDefaultTokenProviders()
                //.AddTokenProvider<ApplicationTokenProvider>("Email")
                .AddRoleManager<ApplicationRoleManager>()
                .AddUserManager<ApplicationUserManager>()
                //.AddEntityFrameworkStores<ApplicationDbContext>()
                .AddSignInManager<ApplicationSignInManager>()
                .AddDapperStores(connString);

            ScopedServices(services);

            TransientServices(services);
        }

        public static void ConfigureElmah(this IServiceCollection services, IConfiguration configuration, IHostingEnvironment env)
        {
            string connString = configuration.GetConnectionString("DefaultConnection");

            //services.AddElmah();
            //services.AddElmah(options =>
            //{
            //    options.Path = "~/elmahurl";
            //    //options.CheckPermissionAction = context => context.User.Identity.IsAuthenticated;
            //});

            ////EmailOptions emailOptions = new EmailOptions();
            //services.AddElmah<XmlFileErrorLog>(options =>
            //{
            //    options.Path = @"errors";
            //    options.LogPath = "~/logs";
            //    //options.Notifiers.Add(new ErrorMailNotifier("Email", emailOptions));
            //});
            services.AddElmah<SqlErrorLog>(options =>
            {
                options.Path = "/errorlog";
                //options.CheckPermissionAction = context => context.User.Identity.IsAuthenticated;

                options.ConnectionString = connString; // DB structure see here: https://bitbucket.org/project-elmah/main/downloads/ELMAH-1.2-db-SQLServer.sql

                //options.Notifiers.Add(new ErrorMailNotifier("Email", emailOptions));
            });
        }

        internal static void ConfigureCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });
            });
        }
    }
}
