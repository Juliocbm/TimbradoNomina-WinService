using System;
using System.Data.SqlClient;
using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Hg.Utils.Exceptions.Connection
{
    /// <summary>
    /// Utilidad para identificar tipos comunes de errores que requieren tratamiento especial.
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Determina si una excepción está relacionada con errores de base de datos.
        /// </summary>
        public static bool IsDatabaseException(Exception ex)
        {
            return ex is SqlException ||
                   ex is DbUpdateException ||
                   ex.InnerException is SqlException;
        }

        /// <summary>
        /// Determina si la excepción indica un timeout (conexión o ejecución prolongada).
        /// </summary>
        public static bool IsTimeout(Exception ex)
        {
            return ex is TimeoutException ||
                   (ex.InnerException is TimeoutException) ||
                   (ex is SqlException sql && sql.Number == -2); // -2: SQL timeout
        }


        /// <summary>
        /// Devuelve una descripción amigable para el tipo de error detectado.
        /// </summary>
        public static string GetFriendlyMessage(Exception ex)
        {
            if (IsTimeout(ex))
                return "⏳ Timeout detectado al acceder a la base de datos.";

            if (IsDatabaseException(ex))
                return "💥 Error de conectividad o transacción con la base de datos.";

            return "⚠️ Error inesperado del sistema.";
        }

        public static string GetFullExceptionDetails(Exception ex, bool asHtml = false)
        {
            var maxLength = asHtml ? int.MaxValue : 5000; // Limitar longitud para texto plano

            if (asHtml)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<div style=\"font-family: Arial, sans-serif; max-width:800px;\">");
                sb.AppendLine("<h2 style=\"color:#d32f2f;margin-bottom:5px;\">🚨 Error detectado</h2>");
                sb.AppendLine($"<p><strong>Descripción:</strong> {WebUtility.HtmlEncode(GetFriendlyMessage(ex))}</p>");
                sb.AppendLine("<div style=\"background:#f9f9f9;border:1px solid #ddd;padding:10px;margin:10px 0;\">");
                sb.AppendLine("<h3 style=\"margin-top:0;\">📋 Detalles técnicos</h3>");
                sb.AppendLine("<pre style=\"white-space:pre-wrap;word-wrap:break-word;margin:0;\">");
                sb.AppendLine(WebUtility.HtmlEncode(ex.ToString().Truncate(maxLength)));
                sb.AppendLine("</pre></div>");

                if (ex.InnerException != null)
                {
                    sb.AppendLine("<div style=\"background:#f0f0f0;border:1px dashed #ccc;padding:10px;margin:10px 0;\">");
                    sb.AppendLine("<h3 style=\"margin-top:0;\">🔍 Excepción interna</h3>");
                    sb.AppendLine("<pre style=\"white-space:pre-wrap;word-wrap:break-word;margin:0;\">");
                    sb.AppendLine(WebUtility.HtmlEncode(ex.InnerException.ToString().Truncate(maxLength)));
                    sb.AppendLine("</pre></div>");
                }

                sb.AppendLine("</div>");
                return sb.ToString();
            }
            else
            {
                var sb = new StringBuilder();
                sb.AppendLine("🚨 Error detectado");
                sb.AppendLine($"Descripción amigable: {GetFriendlyMessage(ex)}");
                sb.AppendLine();
                sb.AppendLine("🧠 Detalles Técnicos:");
                sb.AppendLine(ex.ToString());

                if (ex.InnerException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("🧩 Inner Exception:");
                    sb.AppendLine(ex.InnerException.ToString());
                }

                return sb.ToString();
            }
        }

        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "... [TRUNCATED]";
        }

        /// <summary>
        /// Devuelve el mensaje completo en formato texto plano o HTML.
        /// </summary>
        //public static string GetFullExceptionDetails(Exception ex, bool asHtml = false)
        //{
        //    if (asHtml)
        //    {
        //        var sb = new StringBuilder();
        //        sb.AppendLine("<h2 style=\"color:#d32f2f;\">🚨 Error detectado</h2>");
        //        sb.AppendLine($"<p><strong>Descripción amigable:</strong> {GetFriendlyMessage(ex)}</p>");
        //        sb.AppendLine("<h3>🧠 Detalles Técnicos</h3>");
        //        sb.AppendLine("<pre style=\"background:#f5f5f5;padding:1em;border:1px solid #ddd;border-radius:5px;overflow-x:auto;font-family:monospace;\">");
        //        sb.AppendLine(System.Net.WebUtility.HtmlEncode(ex.ToString()));
        //        sb.AppendLine("</pre>");

        //        if (ex.InnerException != null)
        //        {
        //            sb.AppendLine("<h3>🧩 Inner Exception</h3>");
        //            sb.AppendLine("<pre style=\"background:#f0f0f0;padding:1em;border:1px dashed #ccc;border-radius:5px;overflow-x:auto;font-family:monospace;\">");
        //            sb.AppendLine(System.Net.WebUtility.HtmlEncode(ex.InnerException.ToString()));
        //            sb.AppendLine("</pre>");
        //        }

        //        return sb.ToString();
        //    }
        //    else
        //    {
        //        var sb = new StringBuilder();
        //        sb.AppendLine("🚨 Error detectado");
        //        sb.AppendLine($"Descripción amigable: {GetFriendlyMessage(ex)}");
        //        sb.AppendLine();
        //        sb.AppendLine("🧠 Detalles Técnicos:");
        //        sb.AppendLine(ex.ToString());

        //        if (ex.InnerException != null)
        //        {
        //            sb.AppendLine();
        //            sb.AppendLine("🧩 Inner Exception:");
        //            sb.AppendLine(ex.InnerException.ToString());
        //        }

        //        return sb.ToString();
        //    }
        //}
    }
}
