namespace Nomina.WorkerTimbrado.Models
{
    /// <summary>
    /// Representa la configuración necesaria para el funcionamiento de los servicios de timbrado.
    /// </summary>
    public class WorkerSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string LegacyConnectionString { get; set; } = string.Empty;
        public string ApiBaseUrl { get; set; } = string.Empty;
        public int PollIntervalSeconds { get; set; } = 60;
        public int MaxRetryCount { get; set; } = 3;
        public int BatchSize { get; set; } = 50;
        public int BackoffMinutes { get; set; } = 5;
        public int MigracionIntervalMinutes { get; set; } = 1;
    }
}
