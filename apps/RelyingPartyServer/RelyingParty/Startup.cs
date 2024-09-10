using RelyingParty.Data;
using System.Text.Json.Serialization;
using Amazon.SimpleSystemsManagement;
using RelyingParty.JWT;
using RelyingParty.Middleware;

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
        services.AddSingleton<ParameterStoreService>();
        services.AddAWSService<IAmazonSimpleSystemsManagement>();
        services.AddSingleton<JwtManager>();
        services.AddHttpClient<IEmailService, EmailService>();

        // Add CORS services and configure the policy
        services.AddCors(options =>
        {
            options.AddPolicy("AllowSpecificOrigins",
                builder =>
                {
                    builder.WithOrigins("http://localhost:7123", "http://localhost:8123", "http://localhost:5173")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // Allow credentials explicitly
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
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Encoder =
                    System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            });

        string connectionString = Configuration["ConnectionStrings:Default"];
        services.AddSingleton(new PlanetScaleDatabase(connectionString));

        //Algod
        string host = Configuration["AlgodHTTPApi:host"];
        string token = Configuration["AlgodHTTPApi:token"];
            
        services.AddOpenApiDocument(configure =>
        {
            configure.Version = "v1";
            configure.Title = "ArgonRelyingParty API with Algorand SmartSig delegated access";
            configure.Description = "A client for interfacing with the ArgonRelyingParty API";
            configure.GenerateEnumMappingDescription = false;
            configure.SchemaType = NJsonSchema.SchemaType.OpenApi3;

            // Output the specification in YAML format
            // configure.DocumentProcessors.Add(new YamlDocumentProcessor());
        });
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
        // app.UseCors("AllowAllOrigins");
        app.UseCors("AllowSpecificOrigins");

        app.UseRouting();

        app.UseAuthorization();


        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/",
                async context =>
                {
                    await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
                });
        });

        app.UseOpenApi();
        app.UseSwaggerUI(options => { options.SwaggerEndpoint("/swagger/v1/swagger.json", "Swagger"); });
    }
}