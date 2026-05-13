using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var notes = new List<Note>();
var nextId = 1;

app.MapGet("/", () => Results.Json(new
{
    message = "IsLabApp is running"
}));

app.MapGet("/health", () => Results.Json(new
{
    status = "ok",
    time = DateTime.UtcNow
}));

app.MapGet("/version", (IConfiguration config) => Results.Json(new
{
    name = config["App:Name"] ?? "IsLabApp",
    version = config["App:Version"] ?? "0.1.0-lab4"
}));

app.MapGet("/db/ping", async (IConfiguration config) =>
{
    var connectionString = config.GetConnectionString("Mssql");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Json(new
        {
            status = "error",
            message = "Connection string is empty"
        }, statusCode: 400);
    }

    try
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();

        return Results.Json(new
        {
            status = "ok",
            database = "connected",
            result
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "error",
            message = ex.Message
        }, statusCode: 500);
    }
});

app.MapGet("/api/notes", () => Results.Json(notes));

app.MapGet("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    if (note is null)
    {
        return Results.Json(new
        {
            error = "Note not found"
        }, statusCode: 404);
    }

    return Results.Json(note);
});

app.MapPost("/api/notes", (NoteCreateRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.Json(new
        {
            error = "Title is required"
        }, statusCode: 400);
    }

    var note = new Note(
        nextId++,
        request.Title.Trim(),
        request.Text ?? "",
        DateTime.UtcNow
    );

    notes.Add(note);

    return Results.Json(note, statusCode: 201);
});

app.MapDelete("/api/notes/{id:int}", (int id) =>
{
    var note = notes.FirstOrDefault(n => n.Id == id);

    if (note is null)
    {
        return Results.Json(new
        {
            error = "Note not found"
        }, statusCode: 404);
    }

    notes.Remove(note);

    return Results.Json(new
    {
        deleted = true
    });
});

app.Run();

record Note(int Id, string Title, string Text, DateTime CreatedAt);

record NoteCreateRequest(string Title, string? Text);