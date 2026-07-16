namespace BibliotecaAPI.Dtos;

/// <summary>Payload for creating or updating a livro.</summary>
public record LivroRequest(string Titulo, string Autor, string Categoria, int ExemplaresDisponiveis);

/// <summary>Wire representation returned to clients.</summary>
public record LivroResponse(int Id, string Titulo, string Autor, string Categoria, int ExemplaresDisponiveis);
