using System.Diagnostics;
using System.Text.Json;
using AccessControl.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Facial;

/// <summary>
/// Implementación de <see cref="IFacialService"/> sobre un proceso hijo Python
/// persistente (facial_worker.py, InsightFace/ArcFace). El proceso se arranca al
/// iniciar la app, se mantiene vivo y se reinicia si muere. El acceso se serializa
/// con un semáforo: una operación a la vez es suficiente para la carga real
/// (pico ≈ 5 validaciones/minuto en toda la empresa).
/// </summary>
public class FacialProcessService : IFacialService, IHostedService, IDisposable
{
    private readonly ILogger<FacialProcessService> _logger;
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly string _ejecutablePython;
    private readonly string _rutaWorker;
    private readonly TimeSpan _timeoutOperacion;
    private Process? _proceso;

    public FacialProcessService(IConfiguration configuracion, ILogger<FacialProcessService> logger)
    {
        _logger = logger;
        _ejecutablePython = configuracion["Facial:PythonEjecutable"] ?? "python3";
        _rutaWorker = configuracion["Facial:WorkerScript"]
                      ?? Path.Combine(AppContext.BaseDirectory, "python", "facial_worker.py");
        _timeoutOperacion = TimeSpan.FromSeconds(
            configuracion.GetValue("Facial:TimeoutSegundos", 120));
    }

    // ---- IHostedService ----

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            ArrancarProceso();
        }
        catch (Exception ex)
        {
            // No se tumba la API: el proceso se reintentará en la primera operación.
            _logger.LogError(ex, "No se pudo arrancar el worker facial al inicio.");
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        DetenerProceso();
        return Task.CompletedTask;
    }

    // ---- IFacialService ----

    public async Task<float[]> GenerarEmbedding(byte[] imagen, CancellationToken ct = default)
    {
        var respuesta = await Enviar(new
        {
            op = "embedding",
            image_b64 = Convert.ToBase64String(imagen)
        }, ct);

        var embedding = respuesta.RootElement.GetProperty("embedding");
        var resultado = new float[embedding.GetArrayLength()];
        var i = 0;
        foreach (var valor in embedding.EnumerateArray())
            resultado[i++] = valor.GetSingle();
        return resultado;
    }

    public async Task<double> Comparar(float[] a, float[] b, CancellationToken ct = default)
    {
        var respuesta = await Enviar(new { op = "compare", embedding_a = a, embedding_b = b }, ct);
        return respuesta.RootElement.GetProperty("similitud").GetDouble();
    }

    // ---- Proceso hijo ----

    private async Task<JsonDocument> Enviar(object peticion, CancellationToken ct)
    {
        await _semaforo.WaitAsync(ct);
        try
        {
            if (_proceso is null || _proceso.HasExited)
            {
                _logger.LogWarning("Worker facial caído; reiniciando...");
                ArrancarProceso();
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(_timeoutOperacion);

            string? linea;
            try
            {
                await _proceso!.StandardInput.WriteLineAsync(
                    JsonSerializer.Serialize(peticion).AsMemory(), timeout.Token);
                await _proceso.StandardInput.FlushAsync(timeout.Token);

                // Se ignoran líneas que no sean JSON del protocolo (algunas librerías
                // del worker imprimen diagnósticos por stdout pese a la redirección).
                do
                {
                    linea = await _proceso.StandardOutput.ReadLineAsync(timeout.Token);
                    if (linea is not null && !linea.StartsWith('{'))
                    {
                        _logger.LogDebug("stdout no-protocolo del worker: {Linea}", linea);
                        continue;
                    }
                    break;
                } while (true);
            }
            catch (Exception ex) when (ex is IOException or OperationCanceledException)
            {
                // Canal roto o timeout: se descarta el proceso para que la próxima
                // operación arranque uno nuevo.
                DetenerProceso();
                throw new FacialException("WORKER_NO_DISPONIBLE",
                    "El motor facial no respondió; se reiniciará automáticamente.");
            }

            if (linea is null)
            {
                DetenerProceso();
                throw new FacialException("WORKER_NO_DISPONIBLE", "El motor facial cerró la conexión.");
            }

            var documento = JsonDocument.Parse(linea);
            if (!documento.RootElement.GetProperty("ok").GetBoolean())
            {
                var codigo = documento.RootElement.TryGetProperty("error", out var error)
                    ? error.GetString() ?? "WORKER_ERROR"
                    : "WORKER_ERROR";
                throw new FacialException(codigo);
            }

            return documento;
        }
        finally
        {
            _semaforo.Release();
        }
    }

    private void ArrancarProceso()
    {
        DetenerProceso();

        var inicio = new ProcessStartInfo
        {
            FileName = _ejecutablePython,
            Arguments = _rutaWorker,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _proceso = Process.Start(inicio)
                   ?? throw new InvalidOperationException("No se pudo iniciar el proceso Python.");

        // stderr del worker (carga del modelo, diagnósticos) → log de la API.
        _proceso.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
                _logger.LogInformation("worker: {Mensaje}", e.Data);
        };
        _proceso.BeginErrorReadLine();

        _logger.LogInformation("Worker facial iniciado (pid {Pid}): {Python} {Script}",
            _proceso.Id, _ejecutablePython, _rutaWorker);
    }

    private void DetenerProceso()
    {
        if (_proceso is null)
            return;

        try
        {
            if (!_proceso.HasExited)
                _proceso.Kill(entireProcessTree: true);
        }
        catch
        {
            // El proceso ya murió; no hay nada que limpiar.
        }

        _proceso.Dispose();
        _proceso = null;
    }

    public void Dispose()
    {
        DetenerProceso();
        _semaforo.Dispose();
        GC.SuppressFinalize(this);
    }
}
