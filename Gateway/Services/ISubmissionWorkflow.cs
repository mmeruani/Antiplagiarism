using Microsoft.AspNetCore.Http;

namespace Gateway.Services;

public interface ISubmissionWorkflow
{
    Task<object> SubmitAsync(IFormFile file, string studentId, string studentName, string assignmentId);

    Task<string> GetReportsRawAsync(string workId);

    Task<string> GetWordCloudRawAsync(string reportId);
}