using Fido2NetLib;
using RelyingParty.Algorand.ServerAccount;

namespace RelyingParty;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container
    public void ConfigureServices(IServiceCollection services)
    {
        
        // Add CORS services and configure the policy
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
        });

        // Use the in-memory implementation of IDistributedCache.
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            // Set a short timeout for easy testing.
            options.IdleTimeout = TimeSpan.FromMinutes(2);
            options.Cookie.HttpOnly = true;
            // Strict SameSite mode is required because the default mode used
            // by ASP.NET Core 3 isn't understood by the Conformance Tool
            // and breaks conformance testing
            options.Cookie.SameSite = SameSiteMode.Unspecified;
        });
        services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                    options.JsonSerializerOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
                })
                ;

        string connectionString = Configuration["ConnectionStrings:Default"];
        services.AddSingleton(new PlanetScaleDatabase(connectionString));
        // Transient alternative
        // services.AddTransient<MySqlConnection>(_ => new MySqlConnection());

        services.AddFido2(options =>
        {
            options.ServerDomain = Configuration["fido2:serverDomain"];
            options.ServerName = "FIDO2 Test";
            options.Origins = Configuration.GetSection("fido2:origins").Get<HashSet<string>>();
            options.TimestampDriftTolerance = Configuration.GetValue<int>("fido2:timestampDriftTolerance");
            options.MDSCacheDirPath = Configuration["fido2:MDSCacheDirPath"];
        })
        .AddCachedMetadataService(config =>
        {
            config.AddFidoMetadataRepository(httpClientBuilder =>
            {
                //TODO: any specific config you want for accessing the MDS
            });
        });

        services.AddScoped<IMasterAccount, MasterAccount>();

        services.AddSwaggerGen();

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        app.UseSession();
        app.UseStaticFiles();
        app.UseHttpsRedirection();
        
        // Enable CORS with the policy created in ConfigureServices
        app.UseCors("AllowAllOrigins");

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
            endpoints.MapPost("/test", async context =>
            {
                using var reader = new StreamReader(context.Request.Body);
                string requestBody = await reader.ReadToEndAsync();

                // If you want to parse the JSON into an object, you can use:
                // var jsonObject = JsonConvert.DeserializeObject(requestBody);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(requestBody);
            });
        });

        app.UseSwagger();
        app.UseSwaggerUI();
    }
}