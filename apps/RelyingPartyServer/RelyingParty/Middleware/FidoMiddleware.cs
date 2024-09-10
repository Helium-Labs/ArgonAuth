using Fido2NetLib;

namespace RelyingParty.Middleware;

public class FidoMiddleware
{
    private readonly RequestDelegate _next;
    // reusable static map of origin string name to FIDO2 instance
    private static readonly Dictionary<string, Fido2> _fido2Instances = new();
    public FidoMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Fido2 GetFido2(string origin)
    {
        // Check _fido2Instances for an existing instance
        if (_fido2Instances.TryGetValue(origin, out var fido2Instance))
        {
            return fido2Instance;
        }
        // If no instance exists, create a new one
        fido2Instance = new Fido2(new Fido2Configuration{
            ServerDomain = origin,
            ServerName = "FIDO2 Server",
            Origins = new HashSet<string>{origin},
            TimestampDriftTolerance = 300000,
        });
        // Add the new instance to the map
        _fido2Instances.Add(origin, fido2Instance);
        return fido2Instance;
    }
    
    public async Task Invoke(HttpContext context)
    {
        if (IsTargetController(context))
        {
            // Determine the origin of the request
            var origin = context.Request.Headers["Origin"].ToString();
            if (string.IsNullOrEmpty(origin))
            {
                throw new Exception("Origin header missing from request"); 
            }
            // Instantiate and configure your FIDO2 instance based on the origin
            var fido2Instance = GetFido2(origin);
            // Attach the instance to the HttpContext
            context.Items["Fido2Instance"] = fido2Instance;
        }
        // Call the next delegate/middleware in the pipeline
        await _next(context);
    }
    
    private bool IsTargetController(HttpContext context)
    {
        // Check if the current route matches your specific controller
        // You might need to adjust the logic based on your routing setup
        var routeData = context.GetRouteData();
        var controller = routeData.Values["controller"]?.ToString();
        Console.WriteLine($"Controller!!!!!: {controller}");
        return controller == "FidoController"; // Replace with your controller's name
    }
}