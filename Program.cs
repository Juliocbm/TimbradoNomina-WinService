using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using CFDI.Data.Contexts;
using Nomina.WorkerTimbrado;
using Nomina.WorkerTimbrado.Models;
using Nomina.WorkerTimbrado.Services;
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

        services.AddDbContext<CfdiDbContext>((sp, options) =>
        {
            var settings = sp.GetRequiredService<IOptions<WorkerSettings>>().Value;
            options.UseSqlServer(settings.ConnectionString);
        });

        services.AddTransient<LiquidacionRepository>();

        services.AddHostedService<Worker>();
    });

var host = builder.Build();
await host.RunAsync();
