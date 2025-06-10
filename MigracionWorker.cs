using CFDI.Data.Contexts;
using HG.Utils;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Nomina.WorkerTimbrado.Models;
using TimbradoNominaDataAccess.Repositories;
using TimbradoNominaDataAccess.WebServices;

namespace Nomina.WorkerTimbrado;

/// <summary>
/// Servicio encargado de migrar periódicamente las liquidaciones desde la base
/// de datos legada hacia la actual.
/// </summary>
public class MigracionWorker : BackgroundService
{
    private readonly ILogger<MigracionWorker> _logger;
    private readonly WorkerSettings _settings;
    private readonly LiquidacionRepository _repository;

    /// <summary>
    /// Crea una nueva instancia de <see cref="MigracionWorker"/>.
    /// </summary>
    public MigracionWorker(ILogger<MigracionWorker> logger, IOptions<WorkerSettings> settings, LiquidacionRepository repository)
    {
        _logger = logger;
        _settings = settings.Value;
        _repository = repository;
    }

    /// <summary>
    /// Ejecuta el ciclo de migración de forma periódica mientras el servicio esté activo.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EjecutarMigracionAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MigracionWorker");
            }

            await Task.Delay(TimeSpan.FromMinutes(_settings.MigracionIntervalMinutes), stoppingToken);
        }
    }

    /// <summary>
    /// Ejecuta la operación de migración y registra cuántos registros se copiaron.
    /// </summary>
    /// <param name="ct">Token de cancelación.</param>
    private async Task EjecutarMigracionAsync(CancellationToken ct)
    {
        int nuevasLiquidaciones = 0;
        try
        {
            nuevasLiquidaciones = await _repository.MigrarLiquidacionesToServer2019(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error en migracion de liquidaciones");
        }

        _logger.LogInformation("Migrados {count} registros de liquidacionOperador", nuevasLiquidaciones);
    }
}

