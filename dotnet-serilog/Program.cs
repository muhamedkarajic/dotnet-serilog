using Serilog;
using Serilog.Events;
using SerilogTimings;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    
    app.UseSerilogRequestLogging(configure =>
    {
        configure.MessageTemplate = "HTTP {RequestMethod} {RequestPath} ({UserId}) responded {StatusCode} in {Elapsed:0.0000}ms";
    });
    
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapControllers();
    
    app.MapGet("/ping", () => "pong");

    app.MapGet("/request-context", (IDiagnosticContext diagnosticContext) =>
    {
        diagnosticContext.Set("UserId", "someone");
    });
    
    app.MapGet("/create-customer/{id}", (int id) =>
    {
        Log.Logger.Information("Creating customer id: {id}", id);

        using (Operation.Time("Do some Data Base Query..."))
        {
            Thread.Sleep(500);
        }

        return "Something.";
    });
    
    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "Host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
