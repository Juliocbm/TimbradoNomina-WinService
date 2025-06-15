using Microsoft.EntityFrameworkCore;
using CFDI.Data.Contexts;
using CFDI.Data.Entities;
using Microsoft.Extensions.Configuration;
using HG.Utils;
using System.Runtime;

namespace TimbradoNominaDataAccess.Repositories
{
    /// <summary>
    /// Proporciona operaciones de acceso a datos relacionadas con la entidad <see cref="liquidacionOperador"/>.
    /// </summary>
    public class LiquidacionRepository
    {
        //private readonly CfdiDbContext _context;
        private readonly IConfiguration _configuration;

        private static readonly Dictionary<int, string> _connectionMap = new()
        {
            { 1, "CFDI2019" },
            { 2, "CFDI2008" },
            { 3, "CFDI2008" },
            { 4, "CFDI2008" }
        };

        /// <summary>
        /// Inicializa una nueva instancia del repositorio.
        /// </summary>
        /// <param name="configuration">Origen de configuración con las cadenas de conexión.</param>
        public LiquidacionRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Devuelve el nombre de la cadena de conexión asociada a la compañía dada.
        /// </summary>
        /// <param name="companiaId">Identificador de compañía.</param>
        private string ObtenerNombreConnectionString(int companiaId)
        {
            if (_connectionMap.TryGetValue(companiaId, out var nombre))
            {
                return nombre;
            }
            throw new ArgumentException($"ID de compañía no soportado: {companiaId}");
        }

        /// <summary>
        /// Crea una nueva instancia de <see cref="CfdiDbContext"/> para la compañía indicada.
        /// </summary>
        /// <param name="companiaId">Id de la compañía de la que se desea obtener la conexión.</param>
        private CfdiDbContext CrearContexto(int companiaId = 1)
        {
            var connectionString = DbContextFactory.ObtenerConnectionString(
                companiaId,
                _configuration,
                ObtenerNombreConnectionString);

            return DbContextFactory.Crear<CfdiDbContext>(connectionString);
        }

        /// <summary>
        /// Obtiene un lote de liquidaciones pendientes de timbrar.
        /// </summary>
        /// <param name="batchSize">Número máximo de registros a recuperar.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task<List<liquidacionOperador>> GetPendientesAsync(int batchSize, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            using var _context = CrearContexto();

            var A = await _context.liquidacionOperadors.AsNoTracking()
                .Where(l => (l.Estatus == 0 || (l.Estatus == 4 && l.FechaProximoIntento <= now)) && (l.XMLTimbrado != null || l.PDFTimbrado != null || l.UUID != null))
                .OrderBy(l => l.FechaRegistro)
                .Select(l => new liquidacionOperador
                {
                    IdLiquidacion = l.IdLiquidacion,
                    IdCompania = l.IdCompania,
                    Intentos = l.Intentos,
                    UltimoIntento = l.UltimoIntento
                })
                .Take(batchSize)
                .ToListAsync(ct);

            return A;
        }

        /// <summary>
        /// Marca una liquidación como en proceso e incrementa su contador de intentos.
        /// </summary>
        /// <param name="liq">Liquidación a actualizar.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task<bool> MarcarEnProcesoAsync(liquidacionOperador liq, CancellationToken ct)
        {
            using var _context = CrearContexto();

            var rows = await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania && (l.Estatus == 0 || l.Estatus == 4))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)1)
                    .SetProperty(l => l.Intentos, l => l.Intentos + 1)
                    .SetProperty(l => l.UltimoIntento, l => l.Intentos + 1)
                    .SetProperty(l => l.MensajeCorto, "En proceso"), ct);

            return rows > 0;
        }

        /// <summary>
        /// Marca la liquidación para revisión manual cuando excede los reintentos.
        /// </summary>
        /// <param name="liq">Liquidación afectada.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task SetRequiereRevisionAsync(liquidacionOperador liq, CancellationToken ct)
        {
            using var _context = CrearContexto();
            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)2)
                    .SetProperty(l => l.MensajeCorto, "Requiere revisión"), ct);
        }

        /// <summary>
        /// Registra un error transitorio y agenda el próximo intento.
        /// </summary>
        /// <param name="liq">Liquidación a actualizar.</param>
        /// <param name="backoffMinutes">Minutos que deben pasar antes del próximo intento.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task SetErrorTransitorioAsync(liquidacionOperador liq, int backoffMinutes, CancellationToken ct)
        {
            var next = DateTime.UtcNow.AddMinutes(backoffMinutes);

            using var _context = CrearContexto();

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)4)
                    .SetProperty(l => l.FechaProximoIntento, next)
                    .SetProperty(l => l.MensajeCorto, "Error transitorio. Esperando reintento"), ct);
        }

        /// <summary>
        /// Registra un error permanente o de validación para la liquidación.
        /// </summary>
        /// <param name="liq">Liquidación afectada.</param>
        /// <param name="status">Código de estatus a asignar.</param>
        /// <param name="message">Mensaje de error.</param>
        /// <param name="ct">Token de cancelación.</param>
        public async Task SetErrorAsync(liquidacionOperador liq, int status, string message, CancellationToken ct)
        {
            using var _context = CrearContexto();

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)status)
                    .SetProperty(l => l.MensajeCorto, message), ct);
        }

        /// <summary>
        /// Copia liquidaciones desde la base de datos legada (2008) hacia la actual (2019).
        /// </summary>
        /// <param name="ct">Token de cancelación.</param>
        /// <returns>Número de registros migrados.</returns>
        public async Task<int> MigrarLiquidacionesToServer2019(CancellationToken ct)
        {
            using var origen = CrearContexto(2);
            using var destino = CrearContexto(1);

            var pendientes = await origen.liquidacionOperadors
                .Where(l => l.Estatus == 0)
                .AsNoTracking()
                .ToListAsync(ct);

            if (!pendientes.Any())
                return 0;

            await destino.liquidacionOperadors.AddRangeAsync(pendientes, ct);
            await destino.SaveChangesAsync(ct);

            var ids = pendientes.Select(x => x.IdLiquidacion).ToList();

            var origenParaActualizar = await origen.liquidacionOperadors
                .Where(l => ids.Contains(l.IdLiquidacion))
                .ToListAsync(ct);

            foreach (var item in origenParaActualizar)
            {
                item.Estatus = 8;
                item.MensajeCorto = "Migrada exitosamente";
            }
            await origen.SaveChangesAsync(ct);

            return pendientes.Count;
        }

    }
}
