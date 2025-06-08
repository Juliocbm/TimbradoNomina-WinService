using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace HG.Utils
{
    /// <summary>
    /// Fábrica genérica y reutilizable para crear instancias de DbContext en función de la cadena de conexión.
    /// </summary>
    public static class DbContextFactory
    {
        /// <summary>
        /// Crea una instancia de DbContext utilizando directamente la cadena de conexión proporcionada.
        /// </summary>
        /// <typeparam name="TDbContext">Tipo del DbContext a crear.</typeparam>
        /// <param name="connectionString">Cadena de conexión a utilizar.</param>
        /// <returns>Instancia del DbContext configurada.</returns>
        public static TDbContext Crear<TDbContext>(string connectionString)
            where TDbContext : DbContext
        {
            var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)
                ?? throw new InvalidOperationException($"No se pudo crear una instancia de {typeof(TDbContext).Name}");
        }

        /// <summary>
        /// Obtiene la cadena de conexión asociada a la compañía indicada.
        /// </summary>
        /// <param name="compania">Identificador de la compañía (ej. 1 = HG, 2 = CH, etc).</param>
        /// <param name="configuration">Instancia de configuración (appsettings).</param>
        /// <param name="resolver">
        /// Función que mapea el ID de la compañía a una clave de connection string en el appsettings.
        /// </param>
        /// <returns>Cadena de conexión correspondiente.</returns>
        public static string ObtenerConnectionString(
            int compania,
            IConfiguration configuration,
            Func<int, string> resolver)
        {
            string connectionKey = resolver(compania);
            return configuration.GetConnectionString(connectionKey)
                ?? throw new InvalidOperationException($"No se encontró la cadena de conexión con key: '{connectionKey}'");
        }
    }
}
