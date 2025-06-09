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

        public LiquidacionRepository(IConfiguration configuration 
            //,CfdiDbContext context
            )
        {
            _configuration = configuration;

            //_context = context;
        }

        public async Task<List<liquidacionOperador>> GetPendientesAsync(int batchSize, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            // Obtiene la cadena de conexión de acuerdo con la compañía
            var connectionString = DbContextFactory.ObtenerConnectionString(
                1,
                _configuration,
                companiaId => companiaId switch
                {
                    1 => "CFDI2019",
                    2 => "CFDI2008",
                    3 => "CFDI2008",
                    4 => "CFDI2008",
                    _ => throw new ArgumentException($"ID de compañía no soportado: {companiaId}")
                });

            using var _context = DbContextFactory.Crear<CfdiDbContext>(connectionString);

            var A = await _context.liquidacionOperadors.AsNoTracking()
                .Where(l => l.Estatus == 0 && (l.FechaProximoIntento == null || l.FechaProximoIntento <= now))
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

        public async Task<bool> MarcarEnProcesoAsync(liquidacionOperador liq, CancellationToken ct)
        {
            // Obtiene la cadena de conexión de acuerdo con la compañía
            var connectionString = DbContextFactory.ObtenerConnectionString(
                1,
                _configuration,
                companiaId => companiaId switch
                {
                    1 => "CFDI2019",
                    2 => "CFDI2008",
                    3 => "CFDI2008",
                    4 => "CFDI2008",
                    _ => throw new ArgumentException($"ID de compañía no soportado: {companiaId}")
                });

            using var _context = DbContextFactory.Crear<CfdiDbContext>(connectionString);

            var rows = await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania && l.Estatus == 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)1)
                    .SetProperty(l => l.Intentos, l => l.Intentos + 1)
                    .SetProperty(l => l.UltimoIntento, l => l.Intentos + 1), ct);

            return rows > 0;
        }

        public async Task SetRequiereRevisionAsync(liquidacionOperador liq, CancellationToken ct)
        {
            // Obtiene la cadena de conexión de acuerdo con la compañía
            var connectionString = DbContextFactory.ObtenerConnectionString(
                1,
                _configuration,
                companiaId => companiaId switch
                {
                    1 => "CFDI2019",
                    2 => "CFDI2008",
                    3 => "CFDI2008",
                    4 => "CFDI2008",
                    _ => throw new ArgumentException($"ID de compañía no soportado: {companiaId}")
                });

            using var _context = DbContextFactory.Crear<CfdiDbContext>(connectionString);
            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Estatus, (byte)6), ct);
        }

        public async Task SetErrorTransitorioAsync(liquidacionOperador liq, int backoffMinutes, CancellationToken ct)
        {
            var next = DateTime.UtcNow.AddMinutes(backoffMinutes);

            // Obtiene la cadena de conexión de acuerdo con la compañía
            var connectionString = DbContextFactory.ObtenerConnectionString(
                1,
                _configuration,
                companiaId => companiaId switch
                {
                    1 => "CFDI2019",
                    2 => "CFDI2008",
                    3 => "CFDI2008",
                    4 => "CFDI2008",
                    _ => throw new ArgumentException($"ID de compañía no soportado: {companiaId}")
                });

            using var _context = DbContextFactory.Crear<CfdiDbContext>(connectionString);

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)4)
                    .SetProperty(l => l.FechaProximoIntento, next), ct);
        }

        public async Task SetErrorAsync(liquidacionOperador liq, int status, string message, CancellationToken ct)
        {
            // Obtiene la cadena de conexión de acuerdo con la compañía
            var connectionString = DbContextFactory.ObtenerConnectionString(
                1,
                _configuration,
                companiaId => companiaId switch
                {
                    1 => "CFDI2019",
                    2 => "CFDI2008",
                    3 => "CFDI2008",
                    4 => "CFDI2008",
                    _ => throw new ArgumentException($"ID de compañía no soportado: {companiaId}")
                });

            using var _context = DbContextFactory.Crear<CfdiDbContext>(connectionString);

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)status)
                    .SetProperty(l => l.MensajeCorto, message), ct);
        }
    }
}
