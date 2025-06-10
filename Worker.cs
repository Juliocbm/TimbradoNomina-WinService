using Microsoft.Extensions.Options;
using Nomina.WorkerTimbrado.Models;
using TimbradoNominaDataAccess.Repositories;
using TimbradoNominaDataAccess.WebServices;

namespace Nomina.WorkerTimbrado
{
    /// <summary>
    /// Servicio en segundo plano encargado de timbrar las liquidaciones de
    /// nómina pendientes utilizando <see cref="TimbradoApiClient"/>.
    /// </summary>
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly LiquidacionRepository _repository;
        private readonly WorkerSettings _settings;
        private readonly TimbradoApiClient _apiClient;

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="Worker"/>.
        /// </summary>
        /// <param name="logger">Manejador de logs para el servicio.</param>
        /// <param name="repository">Repositorio de acceso a datos de liquidaciones.</param>
        /// <param name="settings">Configuración del trabajador.</param>
        /// <param name="apiClient">Cliente HTTP utilizado para llamar a la API de timbrado.</param>
        public Worker(ILogger<Worker> logger,
                       LiquidacionRepository repository,
                       IOptions<WorkerSettings> settings,
                       TimbradoApiClient apiClient)
        {
            _logger = logger;
            _repository = repository;
            _settings = settings.Value;
            _apiClient = apiClient;
        }

        /// <summary>
        /// Método principal que se ejecuta en bucle mientras el servicio esté activo.
        /// </summary>
        /// <param name="stoppingToken">Token de cancelación del sistema.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error en ciclo principal");
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.PollIntervalSeconds), stoppingToken);
            }
        }

        /// <summary>
        /// Procesa el lote de liquidaciones pendientes obtenidas desde la base de datos.
        /// </summary>
        /// <param name="ct">Token de cancelación.</param>
        private async Task ProcessPendingAsync(CancellationToken ct)
        {
            var pendientes = await _repository.GetPendientesAsync(_settings.BatchSize, ct);
            _logger.LogInformation("Se encontraron {count} liquidaciones pendientes", pendientes.Count);

            foreach (var liq in pendientes)
            {
                if (liq.Intentos >= _settings.MaxRetryCount)
                {
                    await _repository.SetRequiereRevisionAsync(liq, ct);
                    _logger.LogWarning("Liquidacion {liq} de compania {comp} excede reintentos", liq.IdLiquidacion, liq.IdCompania);
                    continue;
                }

                if (!await _repository.MarcarEnProcesoAsync(liq, ct))
                {
                    _logger.LogInformation("Liquidacion {liq} ya esta siendo procesada", liq.IdLiquidacion);
                    continue;
                }

                try
                {
                    var response = await _apiClient.TimbrarAsync(liq, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("HTTP OK para liquidacion {liq}", liq.IdLiquidacion);
                        continue;
                    }

                    var mensaje = await response.Content.ReadAsStringAsync(ct);
                    int status = response.StatusCode >= System.Net.HttpStatusCode.BadRequest &&
                                 response.StatusCode < System.Net.HttpStatusCode.InternalServerError ? 2 : 3;
                    await _repository.SetErrorAsync(liq, status, mensaje, ct);
                    _logger.LogError("Error respuesta API {status} para liquidacion {liq}", response.StatusCode, liq.IdLiquidacion);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error transitorio para liquidacion {liq}", liq.IdLiquidacion);
                    await _repository.SetErrorTransitorioAsync(liq, _settings.BackoffMinutes, ct);
                }
            }
        }
    }
}
