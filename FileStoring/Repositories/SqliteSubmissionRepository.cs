using FileStoring.Domain;
using Microsoft.Data.Sqlite;

namespace FileStoring.Repositories;

public class SqliteSubmissionRepository : ISubmissionRepository
{
    private readonly string _connectionString;

    public SqliteSubmissionRepository(string connectionString)
    {
        _connectionString = connectionString;
        Initialize();
    }

    private void Initialize()
    {
        using var con = new SqliteConnection(_connectionString);
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS submissions(
              fileId TEXT PRIMARY KEY,
              workId TEXT NOT NULL,
              studentId TEXT NOT NULL,
              studentName TEXT NOT NULL,
              assignmentId TEXT NOT NULL,
              uploadedAt TEXT NOT NULL,
              path TEXT NOT NULL
            );
        """;
        cmd.ExecuteNonQuery();
    }

    public async Task SaveAsync(Submission s)
    {
        await using var con = new SqliteConnection(_connectionString);
        await con.OpenAsync();

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            INSERT INTO submissions(
              fileId, workId, studentId, studentName,
              assignmentId, uploadedAt, path
            )
            VALUES ($f,$w,$sid,$sname,$aid,$ts,$p);
        """;

        cmd.Parameters.AddWithValue("$f", s.FileId);
        cmd.Parameters.AddWithValue("$w", s.WorkId);
        cmd.Parameters.AddWithValue("$sid", s.StudentId);
        cmd.Parameters.AddWithValue("$sname", s.StudentName);
        cmd.Parameters.AddWithValue("$aid", s.AssignmentId);
        cmd.Parameters.AddWithValue("$ts", s.UploadedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$p", s.Path);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<Submission?> GetAsync(string fileId)
    {
        await using var con = new SqliteConnection(_connectionString);
        await con.OpenAsync();

        await using var cmd = con.CreateCommand();
        cmd.CommandText = """
            SELECT fileId, workId, studentId, studentName, assignmentId, uploadedAt, path
            FROM submissions
            WHERE fileId = $f
            LIMIT 1;
        """;
        cmd.Parameters.AddWithValue("$f", fileId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Submission
        {
            FileId = reader.GetString(0),
            WorkId = reader.GetString(1),
            StudentId = reader.GetString(2),
            StudentName = reader.GetString(3),
            AssignmentId = reader.GetString(4),
            UploadedAt = DateTime.Parse(reader.GetString(5)),
            Path = reader.GetString(6)
        };
    }
}
