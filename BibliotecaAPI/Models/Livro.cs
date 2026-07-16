namespace BibliotecaAPI.Models;

public class Livro
{
    public int Id { get; set; }
    public required string Titulo { get; set; }
    public required string Autor { get; set; }
    public required string Categoria { get; set; }
    public int ExemplaresDisponiveis { get; set; }
}
