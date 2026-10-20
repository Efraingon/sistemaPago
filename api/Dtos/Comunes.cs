namespace Monrys.Api.Dtos;

/// <summary>Respuesta paginada estándar: { items, total }.</summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int Total { get; set; }
}

public class Pagina
{
    public int Numero { get; set; } = 1;
    public int Tamano { get; set; } = 20;

    public int Skip => (Math.Max(1, Numero) - 1) * Math.Clamp(Tamano, 1, 200);
    public int Take => Math.Clamp(Tamano, 1, 200);

    public static Pagina Desde(int pagina, int tamano) => new() { Numero = pagina, Tamano = tamano };
}
