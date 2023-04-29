using Fido2NetLib;
using MySql.Data.MySqlClient;

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

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}