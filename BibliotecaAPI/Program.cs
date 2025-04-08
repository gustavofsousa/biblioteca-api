using Npgsql;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});
builder.Services.AddDbContext<BibliotecaContext>(options =>
		    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.MapGet("/livros", () =>
{
    using var conn = new NpgsqlConnection("Host=localhost;Database=biblioteca;Username=biblioteca_user;Password=senha");
    conn.Open();
    using var cmd = new NpgsqlCommand("SELECT titulo FROM livros", conn);
    var reader = cmd.ExecuteReader();
    var livros = new List<string>();
    while (reader.Read())
    {
        livros.Add(reader.GetString(0));
    }
    return livros;
})
.WithName("GetLivros")
.WithOpenApi();

app.Run();
