using CFDI.Data.Contexts;
using HG.Utils;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Nomina.WorkerTimbrado.Models;

namespace Nomina.WorkerTimbrado;

public class MigracionWorker : BackgroundService
{
    private readonly ILogger<MigracionWorker> _logger;
    private readonly WorkerSettings _settings;

    public MigracionWorker(ILogger<MigracionWorker> logger, IOptions<WorkerSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                //await EjecutarMigracionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MigracionWorker");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.MigracionIntervalMinutes), stoppingToken);
        }
    }

    private async Task EjecutarMigracionAsync(CancellationToken ct)
    {
        using var origen = DbContextFactory.Crear<CfdiDbContext>(_settings.LegacyConnectionString);
        using var destino = DbContextFactory.Crear<CfdiDbContext>(_settings.ConnectionString);

        var ultimaFecha = await destino.liquidacionOperadors
            .OrderByDescending(l => l.FechaRegistro)
            .Select(l => (DateTime?)l.FechaRegistro)
            .FirstOrDefaultAsync(ct) ?? DateTime.MinValue;

        var nuevos = await origen.liquidacionOperadors
            .Where(l => l.FechaRegistro > ultimaFecha)
            .AsNoTracking()
            .ToListAsync(ct);

        if (nuevos.Count == 0)
        {
            return;
        }

        await destino.liquidacionOperadors.AddRangeAsync(nuevos, ct);
        await destino.SaveChangesAsync(ct);

        _logger.LogInformation("Migrados {count} registros de liquidacionOperador", nuevos.Count);
    }
}

