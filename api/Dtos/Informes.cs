namespace Monrys.Api.Dtos;

// ---------------------------------------------------------------------------
// MOTOR GENÉRICO DE INFORMES (Centro de Reportes)
// Un informe = catálogo (metadatos + parámetros) + resultado (secciones de
// filas tipadas con totales). El web renderiza cualquier informe con el mismo
// visor, que además permite imprimir y exportar a CSV/Excel.
// ---------------------------------------------------------------------------

/// <summary>Catálogo agrupado por familias (replica la organización de C:\Mundodolar\Reportes).</summary>
public class InformeCatalogoDto
{
    public List<InformeFamiliaDto> Familias { get; set; } = [];
}

public class InformeFamiliaDto
{
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string? Descripcion { get; set; }
    public List<InformeDefDto> Informes { get; set; } = [];
}

public class InformeDefDto
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    /// <summary>Descripción corta del informe (qué muestra y para qué sirve).</summary>
    public string? Descripcion { get; set; }
    /// <summary>Archivo .FRX del VFP del que deriva (trazabilidad).</summary>
    public string? OrigenVfp { get; set; }
    /// <summary>Ruta de la página propia si el informe tiene pantalla dedicada.</summary>
    public string? RutaPantalla { get; set; }
    public List<InformeParametroDto> Parametros { get; set; } = [];
}

public class InformeParametroDto
{
    public string Clave { get; set; } = "";
    public string Etiqueta { get; set; } = "";
    /// <summary>fecha | texto | cliente | banco | moneda | select.</summary>
    public string Tipo { get; set; } = "texto";
    public bool Requerido { get; set; }
    /// <summary>Valor por defecto (ej. 'hoy', 'nacional', una fecha ISO…).</summary>
    public string? Defecto { get; set; }
    public List<InformeOpcionDto> Opciones { get; set; } = [];
}

public class InformeOpcionDto
{
    public string Valor { get; set; } = "";
    public string Etiqueta { get; set; } = "";
}

// ---------------------------------------------------------------------------
// RESULTADO
// ---------------------------------------------------------------------------
public class InformeDto
{
    public string Id { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string? Subtitulo { get; set; }
    /// <summary>Período legible (ej. "Del 01/01/2025 al 31/12/2025").</summary>
    public string? Periodo { get; set; }
    public DateTime Generado { get; set; } = DateTime.Now;
    public InformeEmpresaDto Empresa { get; set; } = new();
    public List<InformeSeccionDto> Secciones { get; set; } = [];
    public List<InformeNotaDto> Notas { get; set; } = [];
}

public class InformeEmpresaDto
{
    public string Nombre { get; set; } = "";
    public string? Rif { get; set; }
    public string? Direccion { get; set; }
    public string? Telefono { get; set; }
}

public class InformeSeccionDto
{
    /// <summary>Nombre de la sección (null = informe plano sin encabezado de grupo).</summary>
    public string? Nombre { get; set; }
    public List<InformeColumnaDto> Columnas { get; set; } = [];
    public List<Dictionary<string, object?>> Filas { get; set; } = [];
    /// <summary>Fila de totales por clave de columna (solo columnas totalizables).</summary>
    public Dictionary<string, object?>? Totales { get; set; }
}

public class InformeColumnaDto
{
    public string Clave { get; set; } = "";
    public string Etiqueta { get; set; } = "";
    /// <summary>texto | numero | moneda | fecha.</summary>
    public string Tipo { get; set; } = "texto";
    /// <summary>Suma de la columna en la fila de totales.</summary>
    public bool Totalizable { get; set; }
}

public class InformeNotaDto
{
    public string Texto { get; set; } = "";
}

// ---------------------------------------------------------------------------
// PARÁMETROS DE EJECUCIÓN (query string)
// ---------------------------------------------------------------------------
public class InformeParametros
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public string? ClienteId { get; set; }
    public string? BancoId { get; set; }
    public string? Cuenta { get; set; }
    public string? Moneda { get; set; }
    public string? Tipo { get; set; }
    public string? Texto { get; set; }
}
