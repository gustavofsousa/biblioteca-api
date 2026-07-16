using BibliotecaAPI.Data;
using BibliotecaAPI.Dtos;
using BibliotecaAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaAPI.Endpoints;

/// <summary>Livro CRUD endpoints backed by EF Core. Wire contract uses DTOs, never entities.</summary>
public static class LivrosEndpoints
{
    public static RouteGroupBuilder MapLivrosEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/livros").WithTags("Livros");

        group.MapGet("/", async (BibliotecaContext db) =>
                await db.Livros
                    .AsNoTracking()
                    .Select(l => new LivroResponse(l.Id, l.Titulo, l.Autor, l.Categoria, l.ExemplaresDisponiveis))
                    .ToListAsync())
            .WithName("GetLivros")
            .WithOpenApi();

        group.MapGet("/{id:int}", async (int id, BibliotecaContext db) =>
                await db.Livros.FindAsync(id) is Livro livro
                    ? Results.Ok(ToResponse(livro))
                    : Results.NotFound())
            .WithName("GetLivroById")
            .WithOpenApi();

        group.MapPost("/", async (LivroRequest req, BibliotecaContext db) =>
            {
                var errors = Validate(req);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                var livro = new Livro
                {
                    Titulo = req.Titulo,
                    Autor = req.Autor,
                    Categoria = req.Categoria,
                    ExemplaresDisponiveis = req.ExemplaresDisponiveis,
                };
                db.Livros.Add(livro);
                await db.SaveChangesAsync();

                return Results.Created($"/livros/{livro.Id}", ToResponse(livro));
            })
            .WithName("CreateLivro")
            .WithOpenApi();

        group.MapPut("/{id:int}", async (int id, LivroRequest req, BibliotecaContext db) =>
            {
                var errors = Validate(req);
                if (errors.Count > 0) return Results.ValidationProblem(errors);

                var livro = await db.Livros.FindAsync(id);
                if (livro is null) return Results.NotFound();

                livro.Titulo = req.Titulo;
                livro.Autor = req.Autor;
                livro.Categoria = req.Categoria;
                livro.ExemplaresDisponiveis = req.ExemplaresDisponiveis;
                await db.SaveChangesAsync();

                return Results.Ok(ToResponse(livro));
            })
            .WithName("UpdateLivro")
            .WithOpenApi();

        group.MapDelete("/{id:int}", async (int id, BibliotecaContext db) =>
            {
                var livro = await db.Livros.FindAsync(id);
                if (livro is null) return Results.NotFound();

                db.Livros.Remove(livro);
                await db.SaveChangesAsync();

                return Results.NoContent();
            })
            .WithName("DeleteLivro")
            .WithOpenApi();

        return group;
    }

    private static LivroResponse ToResponse(Livro l) =>
        new(l.Id, l.Titulo, l.Autor, l.Categoria, l.ExemplaresDisponiveis);

    private static Dictionary<string, string[]> Validate(LivroRequest req)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(req.Titulo))
            errors[nameof(req.Titulo)] = ["Titulo é obrigatório."];
        if (string.IsNullOrWhiteSpace(req.Autor))
            errors[nameof(req.Autor)] = ["Autor é obrigatório."];
        if (string.IsNullOrWhiteSpace(req.Categoria))
            errors[nameof(req.Categoria)] = ["Categoria é obrigatória."];
        if (req.ExemplaresDisponiveis < 0)
            errors[nameof(req.ExemplaresDisponiveis)] = ["ExemplaresDisponiveis não pode ser negativo."];

        return errors;
    }
}
