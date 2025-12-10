using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Gateway.Services;

public class SubmissionWorkflow : ISubmissionWorkflow
{
    private readonly HttpClient _fileStoringClient;
    private readonly HttpClient _analysisClient;

    public SubmissionWorkflow(IHttpClientFactory factory)
    {
        _fileStoringClient = factory.CreateClient("FileStoring");
        if (_fileStoringClient.BaseAddress == null)
        {
            _fileStoringClient.BaseAddress = new Uri("http://localhost:5001");
        }

        _analysisClient = factory.CreateClient("Analysis");
        if (_analysisClient.BaseAddress == null)
        {
            _analysisClient.BaseAddress = new Uri("http://localhost:5002");
        }
    }

     public async Task<object> SubmitAsync(IFormFile file, string studentId, string studentName, string assignmentId)
    {
        using var content = new MultipartFormDataContent();

        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        var uri =
            $"/files?studentId={Uri.EscapeDataString(studentId)}" +
            $"&studentName={Uri.EscapeDataString(studentName)}" +
            $"&assignmentId={Uri.EscapeDataString(assignmentId)}";

        var fsResponse = await _fileStoringClient.PostAsync(uri, content);
        if (!fsResponse.IsSuccessStatusCode)
        {
            var body = await fsResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"FileStoring вернул {(int)fsResponse.StatusCode} {fsResponse.StatusCode}. Тело: {body}");
        }

        var storedJson = await fsResponse.Content.ReadAsStringAsync();
        var stored = JsonSerializer.Deserialize<StoredResult>(storedJson, JsonOptions) ?? throw new InvalidOperationException("Не удалось разобрать ответ FileStoring");
        var createReportBody = new
        {
            fileId    = stored.fileId,
            workId    = stored.workId,
            studentId = studentId
        };

        var anResponse = await _analysisClient.PostAsJsonAsync("/reports", createReportBody, JsonOptions);
        if (!anResponse.IsSuccessStatusCode)
        {
            var body = await anResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"FileAnalysis вернул {(int)anResponse.StatusCode} {anResponse.StatusCode}. Тело: {body}");
        }
        var reportJson = await anResponse.Content.ReadAsStringAsync();
        var report = JsonSerializer.Deserialize<ReportResult>(reportJson, JsonOptions);

        return new
        {
            stored.workId,
            stored.fileId,
            report
        };
    }

    public async Task<string> GetReportsRawAsync(string workId)
    {
        var response = await _analysisClient.GetAsync($"/works/{workId}/reports");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string> GetWordCloudRawAsync(string reportId)
    {
        var response = await _analysisClient.GetAsync($"/reports/{reportId}/wordcloud");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record StoredResult(string fileId, string workId, DateTime storedAt);
    private record ReportResult(string reportId, bool plagiarism, string contentHash, DateTime createdAt);
}
