namespace FileStoring.Domain;

public class Submission
{
    public string FileId { get; set; } = string.Empty;
    public string WorkId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string AssignmentId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Path { get; set; } = string.Empty;

    public Submission() { }

    public Submission(
        string fileId,
        string workId,
        string studentId,
        string studentName,
        string assignmentId,
        DateTime uploadedAt,
        string path,
        string content)
    {
        FileId = fileId;
        WorkId = workId;
        StudentId = studentId;
        StudentName = studentName;
        AssignmentId = assignmentId;
        UploadedAt = uploadedAt;
        Path = path;
    }

    public static Submission Create(
        string workId,
        string studentId,
        string studentName,
        string assignmentId,
        string fileName,
        string content,
        DateTime uploadedAt)
    {
        var fileId = Guid.NewGuid().ToString("N");

        return new Submission
        {
            FileId = fileId,
            WorkId = workId,
            StudentId = studentId,
            StudentName = studentName,
            AssignmentId = assignmentId,
            UploadedAt = uploadedAt,
            Path = fileName,  
        };
    }
}
