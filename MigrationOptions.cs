using System.Text;
using System.Text.Json;

namespace MongDBUpload
{
    /// <summary>
    /// 迁移参数（全部来自本地配置文件 appsettings.json，代码中不保存任何环境值）。
    /// 启动时读取配置文件，界面可修改，点击“开始迁移”/“测试连接”或关闭窗口时自动保存回配置文件。
    /// </summary>
    public class MigrationOptions
    {
        #region 腾讯云 COS

        /// <summary>存储桶，格式：BucketName-APPID（来自配置文件）</summary>
        public string CosBucket { get; set; } = "";

        /// <summary>地域（来自配置文件）</summary>
        public string CosRegion { get; set; } = "";

        /// <summary>腾讯云 APPID（来自配置文件）</summary>
        public string CosAppId { get; set; } = "";

        /// <summary>腾讯云 SecretId（来自配置文件）</summary>
        public string CosSecretId { get; set; } = "";

        /// <summary>腾讯云 SecretKey（来自配置文件）</summary>
        public string CosSecretKey { get; set; } = "";

        /// <summary>对象键前缀，只迁移该前缀下的文件，为空表示全部（来自配置文件）</summary>
        public string CosPrefix { get; set; } = "";

        #endregion

        #region MongoDB

        /// <summary>MongoDB 连接字符串（含账号密码，来自配置文件）</summary>
        public string MongoConnectionString { get; set; } = "";

        /// <summary>MongoDB 数据库名（来自配置文件）</summary>
        public string MongoDatabaseName { get; set; } = "";

        /// <summary>GridFS 桶名，为空使用默认 fs（来自配置文件）</summary>
        public string GridFsBucketName { get; set; } = "";

        #endregion

        #region SQL Server（存放关联表）

        /// <summary>SQL Server 连接字符串（含密码，来自配置文件）</summary>
        public string SqlConnectionString { get; set; } = "";

        /// <summary>关联表表名（来自配置文件）</summary>
        public string MappingTableName { get; set; } = "";

        #endregion

        #region 行为开关（同样保存在配置文件中，可随时修改）

        /// <summary>
        /// 本次最多处理的文件数量（0 = 不限制，处理全部）。
        /// 用于小批量试跑：只从腾讯云读取并同步前 N 个文件到 MongoDB。
        /// </summary>
        public int MaxCount { get; set; }

        /// <summary>是否把 COS 文件内容下载并写入 MongoDB GridFS</summary>
        public bool DownloadFileContent { get; set; } = true;

        /// <summary>是否写入 SQL Server 关联表</summary>
        public bool WriteSqlMapping { get; set; } = true;

        /// <summary>目标已存在时跳过（按 COS 地址去重）</summary>
        public bool SkipExisting { get; set; } = true;

        /// <summary>数据迁移后 MongoDB 文件下载地址的前缀，用于拼接最终地址（来自配置文件）</summary>
        public string MongoUrlPrefix { get; set; } = "";

        #endregion

        #region 配置文件读写

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// 从 JSON 配置文件加载参数；文件不存在时保持当前值（首次启动会用默认值）。
        /// </summary>
        public void LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            string json = File.ReadAllText(path);
            MigrationOptions? loaded = JsonSerializer.Deserialize<MigrationOptions>(json, JsonOptions)
                ?? throw new InvalidOperationException("配置文件内容为空或格式不正确。");

            CosBucket = loaded.CosBucket ?? "";
            CosRegion = loaded.CosRegion ?? "";
            CosAppId = loaded.CosAppId ?? "";
            CosSecretId = loaded.CosSecretId ?? "";
            CosSecretKey = loaded.CosSecretKey ?? "";
            CosPrefix = loaded.CosPrefix ?? "";

            MongoConnectionString = loaded.MongoConnectionString ?? "";
            MongoDatabaseName = loaded.MongoDatabaseName ?? "";
            GridFsBucketName = loaded.GridFsBucketName ?? "";

            SqlConnectionString = loaded.SqlConnectionString ?? "";
            MappingTableName = loaded.MappingTableName ?? "";

            MaxCount = loaded.MaxCount;
            DownloadFileContent = loaded.DownloadFileContent;
            WriteSqlMapping = loaded.WriteSqlMapping;
            SkipExisting = loaded.SkipExisting;
            MongoUrlPrefix = loaded.MongoUrlPrefix ?? "";
        }

        /// <summary>
        /// 把当前参数保存为 JSON 配置文件（UTF-8 无 BOM，格式友好，可直接手工编辑）。
        /// </summary>
        public void SaveToFile(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(path, json, new UTF8Encoding(false));
        }

        #endregion
    }
}