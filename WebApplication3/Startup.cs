using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using WebApplication3.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.PlatformAbstractions;
using System.IO;

namespace WebApplication3
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.Configure<Setting>(options =>
            {
                options.ConnectionString = Configuration.GetSection("MongoConnection:ConnectString").Value;
                options.Database = Configuration.GetSection("MongoConnection:Database").Value;
                options.BaseUrl = Configuration.GetSection("service:baseUrl").Value;
                options.ImageUrl = Configuration.GetSection("service:ImageUrl").Value;
                options.FireBaseDatabase = Configuration.GetSection("service:Firebase:Database").Value;
            });

            // xác thực token
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options => {
                        options.TokenValidationParameters =
                             new TokenValidationParameters
                             {
                                 ValidateIssuer = true,
                                 ValidateAudience = true,
                                 ValidateLifetime = true,
                                 ValidateIssuerSigningKey = true,

                                 ValidIssuer = $"{Configuration.GetSection("Authorize:Issuer").Value}",
                                 ValidAudience = $"{Configuration.GetSection("Authorize:Audience").Value}",
                                 IssuerSigningKeyResolver =
                                  (string token, SecurityToken securityToken, string kid, TokenValidationParameters validationParameters) =>
                                  {
                                      refreshPublicKeys();
                                      return certificates.ContainsKey(kid) ? new List<X509SecurityKey> { new X509SecurityKey(certificates[kid]) } : null;
                                  }
                             };
                    });
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });
            services.ConfigureSwaggerGen(options =>
            {
                options.OperationFilter<AddRequiredHeaderParameter>();
            });
            services.AddMvcCore().AddApiExplorer();

            services.AddCors();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();

            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });

            

            app.UseMvc();
        }
        static Dictionary<string, X509Certificate2> certificates = new Dictionary<string, X509Certificate2>();
        static long expirationTime = 0;

        static void refreshPublicKeys()
        {
            if (Common.Now() >= expirationTime)
            {
                using (var http = new HttpClient())
                {
                    var response = http.GetAsync("https://www.googleapis.com/robot/v1/metadata/x509/securetoken@system.gserviceaccount.com").Result;
                    if (response.IsSuccessStatusCode)
                    {
                        string cacheControlValue = response.Headers.GetValues("cache-control").First();
                        string maxAgeValue = cacheControlValue.Split(',', ' ').First(x => x.StartsWith("max-age")).Split('=')[1];
                        expirationTime = Common.Now() + Convert.ToInt64(maxAgeValue);
                        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content.ReadAsStringAsync().Result);
                        certificates = dictionary.ToDictionary(x => x.Key, x => new X509Certificate2(Encoding.UTF8.GetBytes(x.Value)));
                    }
                }
            }
        }
    }
}
