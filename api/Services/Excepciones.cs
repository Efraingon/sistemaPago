namespace Monrys.Api.Services;

/// <summary>Recurso inexistente → 404.</summary>
public sealed class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>Regla de negocio violada → 400.</summary>
public sealed class ValidacionException : Exception
{
    public ValidacionException(string mensaje) : base(mensaje) { }
}

/// <summary>Conflicto de estado (ej. cierre ya realizado) → 409.</summary>
public sealed class ConflictoException : Exception
{
    public ConflictoException(string mensaje) : base(mensaje) { }
}
