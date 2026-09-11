using Microsoft.Data.SqlClient;
using System.Data;

namespace MongDBUpload
{
    /// <summary>
    /// 关联表一行：一边是腾讯云地址，一边是 MongoDB 地址
    /// </summary>
    public class MappingRow
    {
        public string CosKey { get; set; } = "";
        public string CosUrl { get; set; } = "";
        public string CosPath { get; set; } = "";
        public string MongoDocId { get; set; } = "";
        public string MongoFileId { get; set; } = "";
        public string MongoUrl { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string ETag { get; set; } = "";
        public string Status { get; set; } = "";
        public string ErrorMessage { get; set; } = "";
    }

    /// <summary>
    /// SQL Server 关联表存取（使用 Microsoft.Data.SqlClient）
    /// </summary>
    public class SqlMappingStore
    {
        private readonly string _connectionString;
        private readonly string _table;

        public SqlMappingStore(string connectionString, string tableName)
        {
            _connectionString = connectionString;
            // 表名来自配置文件（mappingTableName），代码里不提供任何默认值
            _table = tableName.Trim();
            if (_table.Length == 0)
            {
                throw new Exception("关联表表名为空，请检查配置文件中的 mappingTableName。");
            }

            // 表名是拼接进 SQL 的，必须先把非法字符挡掉，避免注入
            if (!System.Text.RegularExpressions.Regex.IsMatch(_table, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                throw new Exception($"关联表名不合法：{_table}，只能包含字母、数字和下划线。");
            }
        }

        public string TableName => _table;

        /// <summary>
        /// 测试连接
        /// </summary>
        public void Ping()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT 1", conn);
            cmd.ExecuteScalar();
        }

        /// <summary>
        /// 建表（不存在时创建）+ 建索引
        /// </summary>
        public void EnsureTable()
        {
            string sql = $@"
IF OBJECT_ID(N'dbo.{_table}', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.{_table} (
        Id              BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CosKey          NVARCHAR(1000) NULL,
        CosUrl          NVARCHAR(1000) NULL,
        CosPath         NVARCHAR(1000) NULL,
        MongoDocId      NVARCHAR(64)   NULL,
        MongoFileId     NVARCHAR(64)   NULL,
        MongoUrl        NVARCHAR(1000) NULL,
        FileName        NVARCHAR(400)  NULL,
        FileSize        BIGINT         NULL,
        ETag            NVARCHAR(200)  NULL,
        Status          NVARCHAR(32)   NULL,
        ErrorMessage    NVARCHAR(2000) NULL,
        CreateTime      DATETIME       NOT NULL CONSTRAINT DF_{_table}_CreateTime DEFAULT(GETDATE()),
        UpdateTime      DATETIME       NULL
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_{_table}_CosKey' AND object_id = OBJECT_ID(N'dbo.{_table}'))
    CREATE UNIQUE INDEX UX_{_table}_CosKey ON dbo.{_table}(CosKey);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_{_table}_MongoDocId' AND object_id = OBJECT_ID(N'dbo.{_table}'))
    CREATE INDEX IX_{_table}_MongoDocId ON dbo.{_table}(MongoDocId);
";
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 120;
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 取出已成功迁移的 CosKey 集合（用于跳过重复文件）
        /// </summary>
        public HashSet<string> LoadSuccessKeys()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string sql = $"SELECT CosKey FROM dbo.{_table} WHERE Status = N'Success' AND CosKey IS NOT NULL";
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.CommandTimeout = 300;
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                set.Add(reader.GetString(0));
            }
            return set;
        }

        /// <summary>
        /// 写入（按 CosKey 覆盖更新）
        /// </summary>
        public void Upsert(MappingRow row)
        {
            string sql = $@"
UPDATE dbo.{_table}
   SET CosUrl = @CosUrl, CosPath = @CosPath, MongoDocId = @MongoDocId, MongoFileId = @MongoFileId, MongoUrl = @MongoUrl,
       FileName = @FileName, FileSize = @FileSize, ETag = @ETag, Status = @Status,
       ErrorMessage = @ErrorMessage, UpdateTime = GETDATE()
 WHERE CosKey = @CosKey;

IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO dbo.{_table}
        (CosKey, CosUrl, CosPath, MongoDocId, MongoFileId, MongoUrl, FileName, FileSize, ETag, Status, ErrorMessage, CreateTime)
    VALUES
        (@CosKey, @CosUrl, @CosPath, @MongoDocId, @MongoFileId, @MongoUrl, @FileName, @FileSize, @ETag, @Status, @ErrorMessage, GETDATE());
END
";
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@CosKey", SqlDbType.NVarChar, 1000).Value = Nz(row.CosKey);
            cmd.Parameters.Add("@CosUrl", SqlDbType.NVarChar, 1000).Value = Nz(row.CosUrl);
            cmd.Parameters.Add("@CosPath", SqlDbType.NVarChar, 1000).Value = Nz(row.CosPath);
            cmd.Parameters.Add("@MongoDocId", SqlDbType.NVarChar, 64).Value = Nz(row.MongoDocId);
            cmd.Parameters.Add("@MongoFileId", SqlDbType.NVarChar, 64).Value = Nz(row.MongoFileId);
            cmd.Parameters.Add("@MongoUrl", SqlDbType.NVarChar, 1000).Value = Nz(row.MongoUrl);
            cmd.Parameters.Add("@FileName", SqlDbType.NVarChar, 400).Value = Nz(row.FileName);
            cmd.Parameters.Add("@FileSize", SqlDbType.BigInt).Value = row.FileSize;
            cmd.Parameters.Add("@ETag", SqlDbType.NVarChar, 200).Value = Nz(row.ETag);
            cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 32).Value = Nz(row.Status);
            cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 2000).Value = Nz(row.ErrorMessage, 2000);
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// 批量写入（每 200 条一个事务）
        /// </summary>
        public void UpsertBatch(List<MappingRow> rows)
        {
            if (rows == null || rows.Count == 0) return;
            foreach (var row in rows)
            {
                Upsert(row);
            }
        }

        private static object Nz(string? value, int maxLen = 0)
        {
            if (string.IsNullOrEmpty(value)) return DBNull.Value;
            if (maxLen > 0 && value.Length > maxLen) return value.Substring(0, maxLen);
            return value;
        }
    }
}
