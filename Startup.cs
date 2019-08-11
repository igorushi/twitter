using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Newtonsoft.Json;
using twitter.Services;

namespace twitter
{
    public class Startup
    {
        private readonly ILogger<Startup> logger;

        public Startup(IConfiguration configuration,ILogger<Startup> logger)
        {
            Configuration = configuration;
            this.logger = logger;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureDB(services);
            ConfigureBackgroundServices(services);
            ConfigureMVC(services);
            services.BuildServiceProvider();
        }

        private void ConfigureBackgroundServices(IServiceCollection services)
        {
            var twitCfg = Configuration.GetSection("TwitterCredentials");
            services.AddSingleton<ITweetProvider, TweetProvider>((sp)=>
                    new TweetProvider(twitCfg["ConsumerAPI"], 
                                      twitCfg["ConsumerSecret"],
                                      twitCfg["AccessToken"],
                                      twitCfg["AccessTokenSecret"]
                                      //,useFakeLocation:true // un-comment to get more tweets
                    ));
            services.AddSingleton<IHostedService, TweetLoadingService>();
        }

        private static void ConfigureMVC(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                // Prevent the following exception: 'This method does not support GeometryCollection arguments'
                // See: https://github.com/npgsql/Npgsql.EntityFrameworkCore.PostgreSQL/issues/585
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Point)));
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(LineString)));
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(MultiLineString)));
                options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(Feature)));
            })
            .AddJsonOptions(options =>
            {
                foreach (var converter in GeoJsonSerializer.Create(new GeometryFactory(new PrecisionModel(), 4326)).Converters)
                {
                    options.SerializerSettings.Converters.Add(converter);
                }
            })
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        private void ConfigureDB(IServiceCollection services)
        {
            services
                .AddEntityFrameworkNpgsql()
                .AddDbContext<DB.TweetsContext>(opt =>
                    opt.UseNpgsql(Configuration.GetConnectionString("TweetDBConnection"),
                                    o => o.UseNetTopologySuite()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                ConfigureExcetionHandling(app);
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }

        private void ConfigureExcetionHandling(IApplicationBuilder app)
        {
            app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";

                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        logger.LogError(contextFeature.Error, "Internal Server Error");

                        await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                        {
                            StatusCode = context.Response.StatusCode,
                            Message = "Internal Server Error."
                        }));
                    }
                });
            });
        }
    }
}
