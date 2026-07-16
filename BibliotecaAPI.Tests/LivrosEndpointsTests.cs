using System.Net;
using System.Net.Http.Json;
using BibliotecaAPI.Dtos;

namespace BibliotecaAPI.Tests;

/// <summary>
/// Endpoint tests for Livro CRUD. Each test method gets a fresh factory (fresh InMemory
/// database), so tests are isolated. Assertions derive from spec ACs, not the implementation.
/// </summary>
public class LivrosEndpointsTests
{
    private readonly HttpClient _client = new BibliotecaApiFactory().CreateClient();

    private static LivroRequest ValidRequest() => new("Clean Code", "Robert C. Martin", "Engenharia", 3);

    // AC2 — create then it appears in the list
    [Fact]
    public async Task Post_valid_creates_livro_and_it_appears_in_list()
    {
        var response = await _client.PostAsJsonAsync("/livros", ValidRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<LivroResponse>();
        Assert.NotNull(created);
        Assert.True(created!.Id > 0);
        Assert.Equal("Clean Code", created.Titulo);

        var list = await _client.GetFromJsonAsync<List<LivroResponse>>("/livros");
        Assert.NotNull(list);
        Assert.Contains(list!, l => l.Id == created.Id && l.Titulo == "Clean Code");
    }

    // AC3 — GET by id: found and not found
    [Fact]
    public async Task Get_by_id_returns_livro_when_present_and_404_when_absent()
    {
        var created = await CreateLivroAsync();

        var found = await _client.GetAsync($"/livros/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        var body = await found.Content.ReadFromJsonAsync<LivroResponse>();
        Assert.Equal(created.Id, body!.Id);

        var missing = await _client.GetAsync("/livros/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // AC4 — update persists; unknown id is 404
    [Fact]
    public async Task Put_updates_existing_and_returns_404_for_unknown()
    {
        var created = await CreateLivroAsync();
        var update = new LivroRequest("Clean Architecture", "Robert C. Martin", "Arquitetura", 5);

        var response = await _client.PutAsJsonAsync($"/livros/{created.Id}", update);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var reloaded = await _client.GetFromJsonAsync<LivroResponse>($"/livros/{created.Id}");
        Assert.Equal("Clean Architecture", reloaded!.Titulo);
        Assert.Equal(5, reloaded.ExemplaresDisponiveis);

        var missing = await _client.PutAsJsonAsync("/livros/999999", update);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // AC5 — delete removes; unknown id is 404
    [Fact]
    public async Task Delete_removes_existing_and_returns_404_for_unknown()
    {
        var created = await CreateLivroAsync();

        var response = await _client.DeleteAsync($"/livros/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var afterDelete = await _client.GetAsync($"/livros/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);

        var missing = await _client.DeleteAsync("/livros/999999");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    // AC6 — invalid payloads are rejected and nothing persists
    [Theory]
    [InlineData("", "Autor", "Cat", 1)]
    [InlineData("Titulo", "  ", "Cat", 1)]
    [InlineData("Titulo", "Autor", "", 1)]
    [InlineData("Titulo", "Autor", "Cat", -1)]
    public async Task Post_invalid_payload_returns_400_and_persists_nothing(
        string titulo, string autor, string categoria, int exemplares)
    {
        var response = await _client.PostAsJsonAsync(
            "/livros", new LivroRequest(titulo, autor, categoria, exemplares));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var list = await _client.GetFromJsonAsync<List<LivroResponse>>("/livros");
        Assert.Empty(list!);
    }

    private async Task<LivroResponse> CreateLivroAsync()
    {
        var response = await _client.PostAsJsonAsync("/livros", ValidRequest());
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LivroResponse>())!;
    }
}
