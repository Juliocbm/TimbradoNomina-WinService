using CFDI.Data.Entities;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace TimbradoNominaDataAccess.WebServices
{
    /// <summary>
    /// Cliente HTTP utilizado para invocar el servicio externo de timbrado.
    /// </summary>
    public class TimbradoApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TimbradoApiClient> _logger;

        /// <summary>
        /// Inicializa una nueva instancia del cliente de timbrado.
        /// </summary>
        public TimbradoApiClient(HttpClient httpClient, ILogger<TimbradoApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <summary>
        /// Envía la solicitud de timbrado para la liquidación especificada.
        /// </summary>
        /// <param name="liq">Entidad de liquidación a timbrar.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task<HttpResponseMessage> TimbrarAsync(liquidacionOperador liq, CancellationToken ct)
        {
            var url = $"api/timbrar/{liq.IdLiquidacion}/{liq.IdCompania}";
            try
            {
                _logger.LogInformation("Invocando {Url}", url);
                return await _httpClient.PostAsync(url, null, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error HTTP invocando {Url}", url);
                throw;
            }
        }
    }
}
