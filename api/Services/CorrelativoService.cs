using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Monrys.Api.Data;

namespace Monrys.Api.Services;

public interface ICorrelativoService
{
    /// <summary>Devuelve el siguiente número de correlativo (incremento atómico).</summary>
    Task<long> SiguienteNumeroAsync(string tabla, string ejercicio);

    /// <summary>Devuelve el correlativo con ceros a la izquierda (ej. 10 dígitos → "0000000001").</summary>
    Task<string> SiguienteIdAsync(string tabla, string ejercicio, int digitos);
}

/// <summary>
/// Numeradores transaccionales (reemplazan el MAX()+1 sin candado de VFP).
/// PostgreSQL: INSERT ... ON CONFLICT + UPDATE ... RETURNING (candado de fila).
/// SQLite: UPDATE + lectura dentro de la misma transacción (la BD serializa escrituras).
/// Si ya hay una transacción EF activa se reutiliza; si no, se abre una propia.
/// </summary>
public sealed class CorrelativoService : ICorrelativoService
{
    /// <summary>Contador global (recibos de caja, ids de cliente) → ejercicio "GLOBAL".</summary>
    public const string EjercicioGlobal = "GLOBAL";

    private readonly MonrysDbContext _db;

    public CorrelativoService(MonrysDbContext db) => _db = db;

    public async Task<long> SiguienteNumeroAsync(string tabla, string ejercicio)
    {
        if (string.IsNullOrWhiteSpace(tabla))
            throw new ValidacionException("Debe indicar la tabla del correlativo.");
        if (string.IsNullOrWhiteSpace(ejercicio) || ejercicio.Length > 6)
            throw new ValidacionException("Ejercicio inválido para el correlativo.");

        var esSqlite = _db.Database.IsSqlite();
        var conexion = _db.Database.GetDbConnection();
        var cierreAlFinalizar = conexion.State != ConnectionState.Open;
        if (cierreAlFinalizar) await conexion.OpenAsync();

        var transaccionActual = _db.Database.CurrentTransaction;
        var transaccionPropia = transaccionActual is null;
        var transaccion = transaccionPropia
            ? await conexion.BeginTransactionAsync()
            : transaccionActual!.GetDbTransaction();

        try
        {
            var numero = 0L;

            await using (var insertar = conexion.CreateCommand())
            {
                insertar.Transaction = transaccion;
                insertar.CommandText = esSqlite
                    ? "INSERT OR IGNORE INTO correlativo (tabla, ejercicio, ultimo) VALUES (@t, @e, 0)"
                    : "INSERT INTO correlativo (tabla, ejercicio, ultimo) VALUES (@t, @e, 0) ON CONFLICT (tabla, ejercicio) DO NOTHING";
                AgregarParametro(insertar, "@t", tabla);
                AgregarParametro(insertar, "@e", ejercicio);
                await insertar.ExecuteNonQueryAsync();
            }

            await using (var actualizar = conexion.CreateCommand())
            {
                actualizar.Transaction = transaccion;
                actualizar.CommandText = esSqlite
                    ? "UPDATE correlativo SET ultimo = ultimo + 1 WHERE tabla = @t AND ejercicio = @e;" +
                      "SELECT ultimo FROM correlativo WHERE tabla = @t AND ejercicio = @e"
                    : "UPDATE correlativo SET ultimo = ultimo + 1 WHERE tabla = @t AND ejercicio = @e RETURNING ultimo";
                AgregarParametro(actualizar, "@t", tabla);
                AgregarParametro(actualizar, "@e", ejercicio);

                var resultado = await actualizar.ExecuteScalarAsync();
                if (resultado is null || resultado is DBNull)
                    throw new ValidacionException($"No fue posible obtener el correlativo de {tabla}/{ejercicio}.");
                numero = Convert.ToInt64(resultado, CultureInfo.InvariantCulture);
            }

            if (transaccionPropia) await transaccion.CommitAsync();
            return numero;
        }
        catch
        {
            if (transaccionPropia) await transaccion.RollbackAsync();
            throw;
        }
        finally
        {
            if (transaccionPropia) await transaccion.DisposeAsync();
            if (cierreAlFinalizar) await conexion.CloseAsync();
        }
    }

    public async Task<string> SiguienteIdAsync(string tabla, string ejercicio, int digitos)
    {
        // Hasta 20 dígitos: los recibos de caja usan 20 (VARCHAR(20) del VFP);
        // un correlativo real nunca supera 19 dígitos significativos (máx. de long).
        if (digitos is < 1 or > 20)
            throw new ValidacionException("Cantidad de dígitos de correlativo inválida.");
        var numero = await SiguienteNumeroAsync(tabla, ejercicio);
        return numero.ToString(new string('0', digitos), CultureInfo.InvariantCulture);
    }

    private static void AgregarParametro(IDbCommand comando, string nombre, string valor)
    {
        var parametro = comando.CreateParameter();
        parametro.ParameterName = nombre;
        parametro.Value = valor;
        comando.Parameters.Add(parametro);
    }
}
