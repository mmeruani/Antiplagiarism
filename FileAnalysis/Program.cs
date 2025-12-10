using FileAnalysis.Domain;
using FileAnalysis.Repositories;
using FileAnalysis.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5002");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
Directory.CreateDirectory(dataDir);

var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "data/analysis.db";
builder.Services.AddSingleton<IReportRepository>(new SqliteReportRepository($"Data Source={dbPath}"));
var fileStoringUrl = Environment.GetEnvironmentVariable("FILESTORING_URL") ?? "http://localhost:5001";

builder.Services.AddHttpClient<ITextFetcher, HttpTextFetcher>(client =>
{
    client.BaseAddress = new Uri(fileStoringUrl);
});


builder.Services.AddScoped<IAnalysisService, AnalysisService>();
builder.Services.AddScoped<IWordCloudService, QuickChartWordCloudService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/reports", async (CreateReportRequest request, IAnalysisService service) =>
{
    try
    {
        var report = await service.AnalyseAsync(request);
        return Results.Ok(new
        {
            report.ReportId,
            report.WorkId,
            report.StudentId,
            report.FileId,
            report.ContentHash,
            plagiarism = report.IsPlagiarism,
            report.CreatedAt
        });
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(new { message = ex.Message });
    }
});

app.MapGet("/works/{workId}/reports", async (string workId, IReportRepository repo) =>
{
    var reports = await repo.GetByWorkAsync(workId);
    var result = reports.Select(r => new
    {
        r.ReportId,
        r.StudentId,
        r.FileId,
        r.ContentHash,
        plagiarism = r.IsPlagiarism,
        r.CreatedAt
    });

    return Results.Ok(result);
});

app.MapGet("/reports/{id}", async (string id, IReportRepository repo) =>
{
    var report = await repo.GetAsync(id);
    if (report is null)
        return Results.NotFound();

    return Results.Ok(new
    {
        report.ReportId,
        report.WorkId,
        report.StudentId,
        report.FileId,
        report.ContentHash,
        plagiarism = report.IsPlagiarism,
        report.CreatedAt
    });
});

app.MapGet("/reports/{id}/wordcloud", async (string id, IReportRepository repo, IWordCloudService wc) =>
{
    var report = await repo.GetAsync(id);
    if (report is null)
    {
        return Results.NotFound();
    }

    var url = await wc.BuildUrlAsync(report.FileId);
    return Results.Ok(new { url });
});

app.Run();
