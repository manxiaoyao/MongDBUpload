using System.Diagnostics;

namespace MongDBUpload
{
    /// <summary>
    /// 迁移结果统计
    /// </summary>
    public class MigrationResult
    {
        public int Total { get; set; }
        public int Success { get; set; }
        public int Skipped { get; set; }
        public int Failed { get; set; }
        public long TotalBytes { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan Elapsed { get; set; }
    }

    /// <summary>
    /// 腾讯云 COS -> MongoDB GridFS 迁移 + 关联表写入
    /// </summary>
    public class MigrationService
    {
        private readonly MigrationOptions _opt;
        private readonly Action<string> _log;
        private readonly Action<int, int> _progress;

        public MigrationService(MigrationOptions opt, Action<string> log, Action<int, int> progress)
        {
            _opt = opt;
            _log = log ?? (_ => { });
            _progress = progress ?? ((_, _) => { });
        }

        public MigrationResult Run(CancellationToken token)
        {
            var sw = Stopwatch.StartNew();
            var result = new MigrationResult();

            // 1. 初始化三个存储端
            _log("正在连接腾讯云 COS ...");
            var cos = new CosStorage(_opt);
            _log("腾讯云 COS 连接成功。");

            _log("正在连接 MongoDB ...");
            var mongo = new MongoFileStorage(_opt);
            mongo.Ping();
            _log($"MongoDB 连接成功，数据库：{_opt.MongoDatabaseName}，GridFS 集合：{_opt.GridFsBucketName}。");

            SqlMappingStore? sql = null;
            HashSet<string> doneKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (_opt.WriteSqlMapping)
            {
                _log("正在连接 SQL Server ...");
                sql = new SqlMappingStore(_opt.SqlConnectionString, _opt.MappingTableName);
                sql.Ping();
                sql.EnsureTable();
                _log($"SQL Server 连接成功，关联表 dbo.{sql.TableName} 已就绪。");
                if (_opt.SkipExisting)
                {
                    doneKeys = sql.LoadSuccessKeys();
                    _log($"关联表中已有 {doneKeys.Count} 条成功记录，将跳过已迁移文件。");
                }
            }

            // 2. 列出腾讯云文件
            int limit = _opt.MaxCount;
            _log(string.IsNullOrEmpty(_opt.CosPrefix)
                ? "正在读取腾讯云文件列表（全部）..."
                : $"正在读取腾讯云文件列表（前缀：{_opt.CosPrefix}）...");
            if (limit > 0)
            {
                _log($"本次最多处理 {limit} 个文件（0=全部）。");
            }

            // 限制数量时，若开启了跳过已迁移，已完成的文件也占额度，
            // 因此列表阶段多取一些（额外 2000 个）来补偿被跳过的文件。
            int listLimit = limit > 0
                ? (limit + (_opt.SkipExisting ? 2000 : 0))
                : 0;
            var objects = cos.ListAll(_opt.CosPrefix, _log, token, listLimit);
            _log($"共读取到 {objects.Count} 个文件。");

            if (limit > 0)
            {
                // 列表阶段取多了（补偿跳过用），这里按"实际要处理的文件数"精确截断，
                // 保证进度条和 [i/N] 的 N 就是本次真正要同步的数量。
                int processed = 0;
                int take = objects.Count;
                for (int i = 0; i < objects.Count; i++)
                {
                    if (_opt.SkipExisting && doneKeys.Contains(objects[i].Key)) continue;
                    processed++;
                    if (processed > limit) { take = i; break; }
                }
                if (take < objects.Count)
                {
                    _log($"已达到本次处理上限 {limit} 个，剩余文件本次不再处理（下次运行会接着处理）。");
                    objects = objects.Take(take).ToList();
                }
            }

            result.Total = objects.Count;
            _progress(0, objects.Count);
            if (objects.Count == 0)
            {
                result.Elapsed = sw.Elapsed;
                _log("没有需要迁移的文件，结束。");
                return result;
            }

            var cosNeedDelete = new List<string>();
            int index = 0;

            foreach (var obj in objects)
            {
                token.ThrowIfCancellationRequested();
                index++;

                string cosUrl = obj.Url;
                var row = new MappingRow
                {
                    CosKey = obj.Key,
                    CosUrl = obj.Url,
                    CosPath = obj.Path,
                    FileSize = obj.Size,
                    ETag = obj.ETag,
                    FileName = Path.GetFileName(obj.Key)
                };

                // 跳过已成功的
                if (_opt.SkipExisting && doneKeys.Contains(obj.Key))
                {
                    result.Skipped++;
                    _log($"[{index}/{objects.Count}] 跳过（已迁移）：{obj.Key}");
                    _progress(index, objects.Count);
                    continue;
                }

                try
                {
                    _log($"[{index}/{objects.Count}] 处理：{obj.Key}（{FormatSize(obj.Size)}）");

                    if (!_opt.DownloadFileContent)
                    {
                        // 只写关联关系，不下载文件
                        row.Status = "MappedOnly";
                        row.ErrorMessage = "未下载文件内容";
                        sql?.Upsert(row);
                        result.Success++;
                        _progress(index, objects.Count);
                        continue;
                    }

                    // 下载
                    var data = cos.DownloadBytes(obj.Key, token);
                    if (data == null || data.Length == 0)
                    {
                        throw new Exception("腾讯云返回的文件内容为空。");
                    }

                    // 写入 MongoDB GridFS
                    string fileName = string.IsNullOrEmpty(row.FileName) ? obj.Key : row.FileName;
                    var (docId, fileId, savedName) = mongo.SaveFile(fileName, data, cosUrl, _opt);
                    row.MongoDocId = docId;
                    row.MongoFileId = fileId.ToString();
                    row.MongoUrl = _opt.MongoUrlPrefix + docId;
                    row.FileName = savedName;

                    // 校验：能读回来才算成功
                    var check = mongo.DownloadAsBytes(fileId);
                    if (check == null || check.Length != data.Length)
                    {
                        throw new Exception($"写入 MongoDB 后校验失败：原大小 {data.Length}，读回大小 {check?.Length ?? 0}。");
                    }

                    row.Status = "Success";
                    sql?.Upsert(row);
                    result.Success++;
                    result.TotalBytes += data.Length;

                    if (_opt.DeleteCosSource)
                    {
                        cosNeedDelete.Add(obj.Key);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    row.Status = "Failed";
                    row.ErrorMessage = ex.Message;
                    try
                    {
                        sql?.Upsert(row);
                    }
                    catch (Exception sex)
                    {
                        _log($"  写入关联表也失败：{sex.Message}");
                    }
                    result.Errors.Add($"{obj.Key} -> {ex.Message}");
                    _log($"  失败：{ex.Message}");
                }

                _progress(index, objects.Count);
            }

            // 3. 可选：删除腾讯云源文件
            if (_opt.DeleteCosSource && cosNeedDelete.Count > 0)
            {
                _log($"正在删除腾讯云源文件，共 {cosNeedDelete.Count} 个 ...");
                int deleted = cos.DeleteObjects(cosNeedDelete, _log);
                _log($"腾讯云源文件删除完成，成功 {deleted} 个。");
            }

            result.Elapsed = sw.Elapsed;
            _log("----------------------------------------");
            _log($"迁移完成：总计 {result.Total}，成功 {result.Success}，跳过 {result.Skipped}，失败 {result.Failed}，共 {FormatSize(result.TotalBytes)}，耗时 {result.Elapsed:hh\\:mm\\:ss}。");
            if (result.Errors.Count > 0)
            {
                _log("失败明细：");
                foreach (var e in result.Errors.Take(50))
                {
                    _log("  " + e);
                }
            }
            return result;
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes;
            int i = 0;
            while (v >= 1024 && i < units.Length - 1)
            {
                v /= 1024;
                i++;
            }
            return $"{v:0.##} {units[i]}";
        }
    }
}
