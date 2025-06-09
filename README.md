# 📝 TimbradoNomina Worker Service

Este proyecto implementa un servicio en segundo plano construido sobre **.NET 6** para automatizar el timbrado de liquidaciones de nómina. El servicio consulta periódicamente la base de datos, envía las solicitudes a la API de timbrado y mantiene sincronizada la información proveniente de un sistema legado.

## 📂 Estructura general

```
TimbradoNomina-WinService/
├── Program.cs              # Punto de entrada del servicio
├── MigracionWorker.cs      # Sincronización de datos legacy
├── Worker.cs               # Procesamiento y timbrado de liquidaciones
├── libs/                   # Submódulos reutilizables
│   ├── Cfdi.Data/          # Modelos y DbContext de la BD CFDI
│   └── Utils/              # Utilidades y helpers comunes
└── appsettings*.json       # Archivos de configuración
```

La carpeta **`libs/`** contiene proyectos agregados como submódulos Git que proveen las entidades de base de datos (`Cfdi.Data`) y utilidades compartidas (`Utils`).

## 🔧 Dependencias

- [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
- Paquetes NuGet incluidos en `TimbradoNominaService.csproj`:
  - `Microsoft.Extensions.Hosting`
  - `Microsoft.Extensions.Http`
  - `Microsoft.Extensions.Http.Polly`

## ▶️ Compilación y ejecución

1. Restaurar y compilar:
   ```bash
   dotnet build
   ```
2. Ejecutar el servicio (usando el perfil de desarrollo por defecto):
   ```bash
   dotnet run --project TimbradoNominaService.csproj
   ```
   Ajusta los valores en `appsettings.json` o `appsettings.Development.json` para configurar cadenas de conexión y parámetros del trabajador.

Para instalarlo como servicio de Windows se puede publicar el proyecto y registrar el ejecutable resultante.

## 🚚 MigracionWorker

`MigracionWorker` es un `BackgroundService` que se ejecuta cada minuto. Su función es copiar a la base de datos principal los registros más recientes de `liquidacionOperadors` obtenidos desde una base de datos legada. Utiliza `CfdiDbContext` para leer y escribir las entidades y registra el número de registros migrados en el log.

## 🕒 Worker

`Worker` es el proceso principal encargado de timbrar las liquidaciones pendientes. En cada ciclo:

1. Obtiene un lote de liquidaciones en estado pendiente mediante `LiquidacionRepository`.
2. Marca cada liquidación como *en proceso* y aumenta el contador de intentos.
3. Llama a `TimbradoApiClient` para solicitar el timbrado a la API externa.
4. Dependiendo de la respuesta, actualiza el estatus de la liquidación o agenda un nuevo intento.

Los intervalos de consulta, tamaño de lote y límites de reintentos se controlan con los valores definidos en `WorkerSettings`.

## 🗒️ Mensajes de estatus

El campo `MensajeCorto` refleja el estado actual de cada liquidación:

| Estatus | Mensaje sugerido |
|---------|------------------|
| 1       | En proceso |
| 2       | Error de validación |
| 3       | Error definitivo |
| 4       | Error transitorio. Esperando reintento |
| 6       | Requiere revisión |

