using Fido2NetLib;


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

        services.AddControllers();

        string connectionString = Configuration["ConnectionStrings:Default"];
        services.AddSingleton(new PlanetScaleDatabase(connectionString));
        // Transient alternative
        // services.AddTransient<MySqlConnection>(_ => new MySqlConnection());

        // Register IFido2 as Singleton with Fido2 implementation
        services.AddSingleton<IFido2, Fido2>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

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
    }
}