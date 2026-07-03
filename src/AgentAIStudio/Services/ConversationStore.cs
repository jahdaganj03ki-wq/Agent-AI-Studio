using Microsoft.Data.Sqlite;
using AgentAIStudio.Models;

namespace AgentAIStudio.Services;

public class ConversationStore
{
    private readonly string _connectionString;

    public ConversationStore()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AgentAIStudio", "conversations.db");
        var dir = Path.GetDirectoryName(dbPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Conversations (
                Id TEXT PRIMARY KEY,
                Category TEXT NOT NULL,
                Title TEXT NOT NULL DEFAULT '',
                CreatedAt TEXT NOT NULL,
                UpdatedAt TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Messages (
                Id TEXT PRIMARY KEY,
                ConversationId TEXT NOT NULL REFERENCES Conversations(Id) ON DELETE CASCADE,
                ParentId TEXT REFERENCES Messages(Id),
                BranchIndex INTEGER NOT NULL DEFAULT 0,
                SortOrder INTEGER NOT NULL,
                Role TEXT NOT NULL,
                Prompt TEXT NOT NULL DEFAULT '',
                GeneratedUrl TEXT,
                Base64Thumb TEXT,
                VideoId TEXT,
                MediaType TEXT NOT NULL DEFAULT 'none',
                Status TEXT NOT NULL DEFAULT 'pending',
                ParametersJson TEXT,
                Timestamp TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Messages_ConversationId ON Messages(ConversationId);
            CREATE INDEX IF NOT EXISTS IX_Messages_ParentId ON Messages(ParentId);
        ";
        cmd.ExecuteNonQuery();
    }

    public async Task<List<Conversation>> GetConversationsAsync(string category)
    {
        await Task.CompletedTask;
        var result = new List<Conversation>();
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Category, Title, CreatedAt, UpdatedAt FROM Conversations WHERE Category = @cat ORDER BY UpdatedAt DESC";
        cmd.Parameters.AddWithValue("@cat", category);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Conversation
            {
                Id = Guid.Parse(reader.GetString(0)),
                Category = reader.GetString(1),
                Title = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                UpdatedAt = DateTime.Parse(reader.GetString(4))
            });
        }
        return result;
    }

    public async Task<Conversation?> GetConversationAsync(Guid id)
    {
        await Task.CompletedTask;
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Category, Title, CreatedAt, UpdatedAt FROM Conversations WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        var conv = new Conversation
        {
            Id = Guid.Parse(reader.GetString(0)),
            Category = reader.GetString(1),
            Title = reader.GetString(2),
            CreatedAt = DateTime.Parse(reader.GetString(3)),
            UpdatedAt = DateTime.Parse(reader.GetString(4))
        };

        conv.Messages = await GetMessagesAsync(conn, id);
        return conv;
    }

    public async Task SaveConversationAsync(Conversation conv)
    {
        await Task.CompletedTask;
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT OR REPLACE INTO Conversations (Id, Category, Title, CreatedAt, UpdatedAt)
            VALUES (@id, @cat, @title, @created, @updated)";
        cmd.Parameters.AddWithValue("@id", conv.Id.ToString());
        cmd.Parameters.AddWithValue("@cat", conv.Category);
        cmd.Parameters.AddWithValue("@title", conv.Title);
        cmd.Parameters.AddWithValue("@created", conv.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();

        foreach (var msg in conv.Messages)
        {
            using var msgCmd = conn.CreateCommand();
            msgCmd.CommandText = @"
                INSERT OR REPLACE INTO Messages
                (Id, ConversationId, ParentId, BranchIndex, SortOrder, Role, Prompt,
                 GeneratedUrl, Base64Thumb, VideoId, MediaType, Status, ParametersJson, Timestamp)
                VALUES (@id, @convId, @parentId, @branchIdx, @sort, @role, @prompt,
                        @genUrl, @b64Thumb, @videoId, @mediaType, @status, @params, @ts)";
            msgCmd.Parameters.AddWithValue("@id", msg.Id.ToString());
            msgCmd.Parameters.AddWithValue("@convId", conv.Id.ToString());
            msgCmd.Parameters.AddWithValue("@parentId", (object?)msg.ParentId?.ToString() ?? DBNull.Value);
            msgCmd.Parameters.AddWithValue("@branchIdx", msg.BranchIndex);
            msgCmd.Parameters.AddWithValue("@sort", msg.SortOrder);
            msgCmd.Parameters.AddWithValue("@role", msg.Role);
            msgCmd.Parameters.AddWithValue("@prompt", msg.Prompt);
            msgCmd.Parameters.AddWithValue("@genUrl", (object?)msg.GeneratedUrl ?? DBNull.Value);
            msgCmd.Parameters.AddWithValue("@b64Thumb", (object?)msg.Base64Thumb ?? DBNull.Value);
            msgCmd.Parameters.AddWithValue("@videoId", (object?)msg.VideoId ?? DBNull.Value);
            msgCmd.Parameters.AddWithValue("@mediaType", msg.MediaType.ToString().ToLower());
            msgCmd.Parameters.AddWithValue("@status", msg.Status.ToString().ToLower());
            msgCmd.Parameters.AddWithValue("@params", (object?)msg.ParametersJson ?? DBNull.Value);
            msgCmd.Parameters.AddWithValue("@ts", msg.Timestamp.ToString("O"));
            msgCmd.ExecuteNonQuery();
        }
    }

    public async Task DeleteConversationAsync(Guid id)
    {
        await Task.CompletedTask;
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Conversations WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.ExecuteNonQuery();
    }

    public async Task UpdateTitleAsync(Guid id, string title)
    {
        await Task.CompletedTask;
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Conversations SET Title = @title, UpdatedAt = @updated WHERE Id = @id";
        cmd.Parameters.AddWithValue("@id", id.ToString());
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    private static async Task<List<Message>> GetMessagesAsync(SqliteConnection conn, Guid conversationId)
    {
        var messages = new List<Message>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, ConversationId, ParentId, BranchIndex, SortOrder, Role, Prompt,
                   GeneratedUrl, Base64Thumb, VideoId, MediaType, Status, ParametersJson, Timestamp
            FROM Messages WHERE ConversationId = @convId ORDER BY SortOrder";
        cmd.Parameters.AddWithValue("@convId", conversationId.ToString());

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            messages.Add(new Message
            {
                Id = Guid.Parse(reader.GetString(0)),
                ConversationId = Guid.Parse(reader.GetString(1)),
                ParentId = reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
                BranchIndex = reader.GetInt32(3),
                SortOrder = reader.GetInt32(4),
                Role = reader.GetString(5),
                Prompt = reader.GetString(6),
                GeneratedUrl = reader.IsDBNull(7) ? null : reader.GetString(7),
                Base64Thumb = reader.IsDBNull(8) ? null : reader.GetString(8),
                VideoId = reader.IsDBNull(9) ? null : reader.GetString(9),
                MediaType = Enum.Parse<MediaType>(reader.GetString(10), true),
                Status = Enum.Parse<MessageStatus>(reader.GetString(11), true),
                ParametersJson = reader.IsDBNull(12) ? null : reader.GetString(12),
                Timestamp = DateTime.Parse(reader.GetString(13))
            });
        }
        return messages;
    }

    public static string ParametersToJson(GenerationParameters parameters, string model)
    {
        var dict = new Dictionary<string, object>
        {
            ["model"] = model,
            ["temperature"] = parameters.Temperature,
            ["top_p"] = parameters.TopP,
            ["max_tokens"] = parameters.MaxTokens,
            ["size"] = parameters.Size,
        };
        if (parameters.Seed.HasValue) dict["seed"] = parameters.Seed.Value;
        if (parameters.EnableThinking) dict["thinking"] = true;
        return System.Text.Json.JsonSerializer.Serialize(dict);
    }
}
