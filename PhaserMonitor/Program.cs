
using Photino.NET;
using PhaserMonitor.Services;


class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(Program).Assembly.FullName
        });

        builder.Services.AddScoped<PhaserConfigManager>();
        builder.Services.AddAuthorization(options =>
        {
            // Simple role-based policy
            options.AddPolicy("RequireFactoryOrAdmin", policy =>
                policy.RequireRole("Factory", "Administrator"));
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Error);

        builder.WebHost.UseKestrel();
        builder.WebHost.UseUrls("http://0.0.0.0:5000");

        builder.Services.AddHttpClient();
        // Configuração do CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder => builder
                .SetIsOriginAllowed((host) => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
        });

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v2", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Minha API",
                Version = "v2"
            });
        });


        builder.Services.AddSignalR();
        var app = builder.Build();

        app.UseDefaultFiles(new DefaultFilesOptions
        {
            DefaultFileNames = new List<string> { "index.html" }
        });
        app.UseStaticFiles();
        app.MapFallbackToFile("index.html");

        app.UseRouting();
        app.UseCors("CorsPolicy");

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v2/swagger.json", "Phaser Monitor");
            });
        }

        app.UseAuthorization();
        app.MapControllers();


        var serverThread = new Thread(() => app.Run());
        serverThread.Start();

        var iconPath = Path.Combine(AppContext.BaseDirectory, "icon.ico");

        var window = new PhotinoWindow()
    .SetTitle("Phaser Status Monitor")
    .SetUseOsDefaultSize(false)
    .SetSize(1200, 800)
    .Center()
    .SetIconFile("./icon.ico")
    .SetResizable(true)
    .Load("http://localhost:5000/");

        window.WaitForClose();

        Environment.Exit(0);
    }
}



