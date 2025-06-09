using Microsoft.EntityFrameworkCore;
using CFDI.Data.Contexts;
using CFDI.Data.Entities;
using Microsoft.Extensions.Configuration;
using HG.Utils;

namespace TimbradoNominaDataAccess.Repositories
{
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

        public LiquidacionRepository(IConfiguration configuration
            //,CfdiDbContext context
            )
        {
            _configuration = configuration;

            //_context = context;
        }

        private string ObtenerNombreConnectionString(int companiaId)
        {
            if (_connectionMap.TryGetValue(companiaId, out var nombre))
            {
                return nombre;
            }
            throw new ArgumentException($"ID de compañía no soportado: {companiaId}");
        }

        private CfdiDbContext CrearContexto(int companiaId = 1)
        {
            var connectionString = DbContextFactory.ObtenerConnectionString(
                companiaId,
                _configuration,
                ObtenerNombreConnectionString);

            return DbContextFactory.Crear<CfdiDbContext>(connectionString);
        }

        public async Task<List<liquidacionOperador>> GetPendientesAsync(int batchSize, CancellationToken ct, int companiaId = 1)
        {
            var now = DateTime.UtcNow;

            using var _context = CrearContexto(companiaId);

            var A = await _context.liquidacionOperadors.AsNoTracking()
                .Where(l => l.Estatus == 0 || (l.Estatus == 4 && l.FechaProximoIntento <= now))
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

        public async Task<bool> MarcarEnProcesoAsync(liquidacionOperador liq, CancellationToken ct, int companiaId = 1)
        {
            using var _context = CrearContexto(companiaId);

            var rows = await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania && (l.Estatus == 0 || l.Estatus == 4))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)1)
                    .SetProperty(l => l.Intentos, l => l.Intentos + 1)
                    .SetProperty(l => l.UltimoIntento, l => l.Intentos + 1), ct);

            return rows > 0;
        }

        public async Task SetRequiereRevisionAsync(liquidacionOperador liq, CancellationToken ct, int companiaId = 1)
        {
            using var _context = CrearContexto(companiaId);
            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Estatus, (byte)6), ct);
        }

        public async Task SetErrorTransitorioAsync(liquidacionOperador liq, int backoffMinutes, CancellationToken ct, int companiaId = 1)
        {
            var next = DateTime.UtcNow.AddMinutes(backoffMinutes);

            using var _context = CrearContexto(companiaId);

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)4)
                    .SetProperty(l => l.FechaProximoIntento, next), ct);
        }

        public async Task SetErrorAsync(liquidacionOperador liq, int status, string message, CancellationToken ct, int companiaId = 1)
        {
            using var _context = CrearContexto(companiaId);

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)status)
                    .SetProperty(l => l.MensajeCorto, message), ct);
        }
    }
}
