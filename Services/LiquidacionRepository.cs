using Microsoft.EntityFrameworkCore;
using CFDI.Data.Contexts;
using Nomina.WorkerTimbrado.Models;

namespace Nomina.WorkerTimbrado.Services
{
    public class LiquidacionRepository
    {
        private readonly CfdiDbContext _context;

        public LiquidacionRepository(CfdiDbContext context)
        {
            _context = context;
        }

        public async Task<List<Liquidacion>> GetPendientesAsync(int batchSize, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            return await _context.liquidacionOperadors.AsNoTracking()
                .Where(l => l.Estatus == 0 && (l.FechaProximoIntento == null || l.FechaProximoIntento <= now))
                .OrderBy(l => l.FechaRegistro)
                .Select(l => new Liquidacion
                {
                    IdLiquidacion = l.IdLiquidacion,
                    IdCompania = l.IdCompania,
                    Intentos = l.Intentos,
                    UltimoIntento = l.UltimoIntento
                })
                .Take(batchSize)
                .ToListAsync(ct);
        }

        public async Task<bool> MarcarEnProcesoAsync(Liquidacion liq, CancellationToken ct)
        {
            var rows = await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania && l.Estatus == 0)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)1)
                    .SetProperty(l => l.Intentos, l => l.Intentos + 1)
                    .SetProperty(l => l.UltimoIntento, l => l.Intentos + 1), ct);

            return rows > 0;
        }

        public async Task SetRequiereRevisionAsync(Liquidacion liq, CancellationToken ct)
        {
            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.Estatus, (byte)6), ct);
        }

        public async Task SetErrorTransitorioAsync(Liquidacion liq, int backoffMinutes, CancellationToken ct)
        {
            var next = DateTime.UtcNow.AddMinutes(backoffMinutes);

            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)4)
                    .SetProperty(l => l.FechaProximoIntento, next), ct);
        }

        public async Task SetErrorAsync(Liquidacion liq, int status, string message, CancellationToken ct)
        {
            await _context.liquidacionOperadors
                .Where(l => l.IdLiquidacion == liq.IdLiquidacion && l.IdCompania == liq.IdCompania)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.Estatus, (byte)status)
                    .SetProperty(l => l.MensajeCorto, message), ct);
        }
    }
}
