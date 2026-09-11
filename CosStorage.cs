using COSXML;
using COSXML.Auth;
using COSXML.Model.Bucket;
using COSXML.Model.Object;
using COSXML.Model.Tag;

namespace MongDBUpload
{
    /// <summary>
    /// 一条 COS 对象信息
    /// </summary>
    public class CosObjectInfo
    {
        /// <summary>对象键（即 COS 上的路径，如 upload/2024/a.pdf）</summary>
        public string Key { get; set; } = "";

        /// <summary>带存储桶主机的完整访问地址</summary>
        public string Url { get; set; } = "";

        /// <summary>带前导斜杠的路径形式，如 /upload/2024/a.pdf</summary>
        public string Path { get; set; } = "";

        /// <summary>文件大小（字节）</summary>
        public long Size { get; set; }

        public string ETag { get; set; } = "";

        public string LastModified { get; set; } = "";
    }

    /// <summary>
    /// 腾讯云 COS 访问封装（永久密钥）
    /// </summary>
    public class CosStorage
    {
        private readonly MigrationOptions _opt;
        private readonly CosXml _cosXml;

        public CosStorage(MigrationOptions opt)
        {
            _opt = opt;

            CosXmlConfig config = new CosXmlConfig.Builder()
                .IsHttps(true)
                .SetRegion(opt.CosRegion)
                .SetDebugLog(false)
                .Build();

            var provider = new DefaultQCloudCredentialProvider(opt.CosSecretId, opt.CosSecretKey, 600);
            _cosXml = new CosXmlServer(config, provider);
        }

        /// <summary>
        /// 分页列出存储桶内的全部对象（自动穿透目录）
        /// </summary>
        /// <param name="prefix">对象前缀，可为空</param>
        /// <param name="log">日志回调，可为空</param>
        /// <param name="token">取消标记</param>
        /// <param name="maxCount">最多返回数量，0 表示不限制（用于测试连接）</param>
        public List<CosObjectInfo> ListAll(string prefix, Action<string>? log, CancellationToken token, int maxCount = 0)
        {
            var list = new List<CosObjectInfo>();
            string marker = "";
            int page = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();

                GetBucketRequest request = new GetBucketRequest(_opt.CosBucket);
                request.SetMaxKeys("1000");
                if (!string.IsNullOrEmpty(prefix))
                {
                    request.SetPrefix(prefix);
                }
                if (!string.IsNullOrEmpty(marker))
                {
                    request.SetMarker(marker);
                }

                GetBucketResult result = _cosXml.GetBucket(request);
                ListBucket info = result.listBucket;

                if (info == null)
                {
                    throw new Exception("读取存储桶列表失败：返回内容为空。请检查 Bucket / Region / 密钥 是否正确。响应：" + result.RawContentBodyString);
                }

                if (info.contentsList != null)
                {
                    foreach (var item in info.contentsList)
                    {
                        if (string.IsNullOrEmpty(item.key)) continue;

                        // 目录占位对象大小为 0 且以 / 结尾，跳过
                        if (item.key.EndsWith("/") && item.size == 0) continue;

                        list.Add(new CosObjectInfo
                        {
                            Key = item.key,
                            Url = GetObjectUrl(item.key),
                            Path = "/" + item.key,
                            Size = item.size,
                            ETag = (item.eTag ?? "").Trim('"'),
                            LastModified = item.lastModified ?? ""
                        });

                        if (maxCount > 0 && list.Count >= maxCount)
                        {
                            return list;
                        }
                    }
                }

                page++;
                log?.Invoke($"已扫描第 {page} 页，累计 {list.Count} 个对象...");

                if (!info.isTruncated) break;

                marker = info.nextMarker;
                if (string.IsNullOrEmpty(marker))
                {
                    // 兜底：部分情况下未返回 nextMarker，用最后一个 key 继续翻页
                    marker = list.Count > 0 ? list[^1].Key : "";
                    if (string.IsNullOrEmpty(marker)) break;
                }
            }

            return list;
        }

        /// <summary>
        /// 下载对象内容
        /// </summary>
        public byte[] DownloadBytes(string key, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            GetObjectBytesRequest request = new GetObjectBytesRequest(bucket: _opt.CosBucket, key: key);
            GetObjectBytesResult result = _cosXml.GetObject(request);
            if (result.httpCode == 200 && result.content != null)
            {
                return result.content;
            }
            throw new Exception($"下载失败 HTTP {result.httpCode}：{result.RawContentBodyString}");
        }

        /// <summary>
        /// 生成对象的访问地址
        /// </summary>
        public string GetObjectUrl(string key)
        {
            try
            {
                return _cosXml.GetObjectUrl(_opt.CosBucket, key);
            }
            catch
            {
                return $"https://{_opt.CosBucket}.cos.{_opt.CosRegion}.myqcloud.com/{key}";
            }
        }

        /// <summary>
        /// 批量删除对象，返回删除请求是否成功
        /// </summary>
        public int DeleteObjects(List<string> keys, Action<string>? log = null)
        {
            if (keys == null || keys.Count == 0) return 0;

            int total = 0;
            // COS 单次批量删除上限 1000，分批处理
            for (int i = 0; i < keys.Count; i += 1000)
            {
                var batch = keys.Skip(i).Take(1000).ToList();
                DeleteMultiObjectRequest request = new DeleteMultiObjectRequest(bucket: _opt.CosBucket);
                request.SetDeleteQuiet(false);
                request.SetObjectKeys(batch);
                _cosXml.DeleteMultiObjects(request);
                total += batch.Count;
                log?.Invoke($"  已删除 {total}/{keys.Count} 个对象...");
            }
            return total;
        }
    }
}
