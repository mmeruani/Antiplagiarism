using Gateway.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var fileStoringUrl = Environment.GetEnvironmentVariable("FILESTORING_URL") ?? builder.Configuration["FILESTORING_URL"] ?? "http://localhost:5001";
var fileAnalysisUrl = Environment.GetEnvironmentVariable("FILEANALYSIS_URL") ?? builder.Configuration["FILEANALYSIS_URL"] ?? "http://localhost:5002";

builder.Services.AddHttpClient("FileStoring", client =>
{
    client.BaseAddress = new Uri(fileStoringUrl);
});

builder.Services.AddHttpClient("Analysis", client =>
{
    client.BaseAddress = new Uri(fileAnalysisUrl);
});

builder.Services.AddScoped<ISubmissionWorkflow, SubmissionWorkflow>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway v1");
});

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapPost("/api/submit",
        async ([FromForm] SubmitRequest request,
            ISubmissionWorkflow workflow) =>
        {
            var file = request.File;
            var studentName = request.StudentName;
            var assignmentName = request.AssignmentName;

            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { message = "Файл не передан" });
            }

            if (!file.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new { message = "Поддерживаются только файлы .txt" });
            }

            studentName = studentName?.Trim() ?? string.Empty;
            assignmentName = assignmentName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(studentName))
            {
                return Results.BadRequest(new { message = "Имя студента обязательно" });
            }

            if (string.IsNullOrWhiteSpace(assignmentName))
            {
                return Results.BadRequest(new { message = "Название задания обязательно" });
            }

            var studentId = Guid.NewGuid().ToString("N");
            var assignmentId = assignmentName;

            try
            {
                var result = await workflow.SubmitAsync(file, studentId, studentName, assignmentId);
                return Results.Ok(result);
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem(
                    title: "Один из внутренних сервисов недоступен",
                    detail: ex.Message,
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
    .DisableAntiforgery()
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/api/works/{workId}/reports",
        async (string workId, ISubmissionWorkflow workflow) =>
        {
            var json = await workflow.GetReportsRawAsync(workId);
            return Results.Content(json, "application/json");
        })
    .Produces(StatusCodes.Status200OK);

app.MapGet("/api/reports/{reportId}/wordcloud",
        async (string reportId, ISubmissionWorkflow workflow) =>
        {
            var json = await workflow.GetWordCloudRawAsync(reportId);
            return Results.Content(json, "application/json");
        })
    .Produces(StatusCodes.Status200OK);

app.Run();

public class SubmitRequest
{
    public IFormFile File { get; set; } = default!;
    public string StudentName { get; set; } = string.Empty;
    public string AssignmentName { get; set; } = string.Empty;
}
