using Microsoft.Extensions.Options;
using CFDI.Data.Contexts;
using Nomina.WorkerTimbrado;
using Nomina.WorkerTimbrado.Models;
using Nomina.WorkerTimbrado.Services;
using HG.Utils;
using Polly;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerSettings>(context.Configuration.GetSection("WorkerSettings"));
        services.AddHttpClient<TimbradoApiClient>((sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
            client.BaseAddress = new Uri(settings.ApiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddTransientHttpErrorPolicy(builder =>
            builder.WaitAndRetryAsync(3, retry => TimeSpan.FromSeconds(Math.Pow(2, retry))));

        services.AddTransient<LiquidacionRepository>();

        services.AddScoped<CfdiDbContext>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
            return DbContextFactory.Crear<CfdiDbContext>(settings.ConnectionString);
        });

        services.AddHostedService<Worker>();
        services.AddHostedService<MigracionWorker>();
    });

var host = builder.Build();
await host.RunAsync();
