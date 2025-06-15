using CFDI.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;

namespace TimbradoNominaDataAccess.WebServices
{
    /// <summary>
    /// Cliente HTTP utilizado para invocar el servicio externo de timbrado.
    /// </summary>
    public class TimbradoApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TimbradoApiClient> _logger;
        private const string Endpoint = "api/TimbrarLiquidacion";

        /// <summary>
        /// Inicializa una nueva instancia del cliente de timbrado.
        /// </summary>
        public TimbradoApiClient(HttpClient httpClient, ILogger<TimbradoApiClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Envía la solicitud de timbrado para la liquidación especificada.
        /// </summary>
        /// <param name="liq">Entidad de liquidación a timbrar.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task<HttpResponseMessage> TimbrarAsync(liquidacionOperador liq, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(liq);

            var uri = BuildRequestUri(liq);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);

            try
            {
                _logger.LogInformation("Invocando {Url}", uri);
                return await _httpClient.SendAsync(request, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error HTTP invocando {Url}", uri);
                throw;
            }
        }

        private static Uri BuildRequestUri(liquidacionOperador liq)
        {
            var query = new Dictionary<string, string?>
            {
                ["idCompania"] = liq.IdCompania.ToString(),
                ["noLiquidacion"] = liq.IdLiquidacion.ToString()
            };

            var url = QueryHelpers.AddQueryString(Endpoint, query);
            return new Uri(url, UriKind.Relative);
        }
    }
}
