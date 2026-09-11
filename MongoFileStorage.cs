using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace MongDBUpload
{
    /// <summary>
    /// 与 ERP 系统 UploadDoc 结构保持一致的文档，迁移后 ERP 接口可直接下载。
    /// 集合名固定为 UploadDoc（即 ERP 的 MongodbHelper&lt;UploadDoc&gt; 使用的集合名）。
    /// </summary>
    [BsonIgnoreExtraElements]
    public class UploadDoc
    {
        [BsonId(IdGenerator = typeof(StringObjectIdGenerator))]
        [BsonRepresentation(BsonType.ObjectId)]
        public virtual string Id { get; set; } = "";

        /// <summary>文件名称</summary>
        public string FileName { get; set; } = "";

        /// <summary>业务参考id</summary>
        public string ReferenceId { get; set; } = "";

        /// <summary>用户id</summary>
        public string UserId { get; set; } = "";

        /// <summary>GridFS 文件 id</summary>
        public ObjectId FileId { get; set; }

        /// <summary>上传时间</summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }
    }

    /// <summary>
    /// MongoDB GridFS 存储封装
    /// </summary>
    public class MongoFileStorage
    {
        private readonly IMongoDatabase _database;
        private readonly IGridFSBucket _bucket;

        public string DatabaseName { get; }

        public MongoFileStorage(MigrationOptions opt)
        {
            var client = new MongoClient(opt.MongoConnectionString);

            // 连接串带 ?authSource=admin 这类查询参数时，不能靠字符串截取取库名，优先用显式配置
            string dbName = opt.MongoDatabaseName;
            if (string.IsNullOrWhiteSpace(dbName))
            {
                dbName = MongoUrl.Create(opt.MongoConnectionString).DatabaseName;
            }
            if (string.IsNullOrWhiteSpace(dbName))
            {
                throw new Exception("未能确定 MongoDB 数据库名，请填写“MongoDB 数据库名”。");
            }

            DatabaseName = dbName;
            _database = client.GetDatabase(dbName);

            _bucket = string.IsNullOrWhiteSpace(opt.GridFsBucketName)
                ? new GridFSBucket(_database)
                : new GridFSBucket(_database, new GridFSBucketOptions { BucketName = opt.GridFsBucketName });
        }

        /// <summary>
        /// 连接测试
        /// </summary>
        public void Ping()
        {
            _database.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
        }

        public IMongoCollection<T> GetCollection<T>(string name)
        {
            return _database.GetCollection<T>(name);
        }

        /// <summary>
        /// 把文件内容写入 GridFS，返回 UploadDoc 记录 id（字符串）与 GridFS 文件 id
        /// </summary>
        public (string docId, ObjectId fileId, string fileName) SaveFile(string fileName, byte[] data, string cosUrl, MigrationOptions opt)
        {
            ObjectId fileId = _bucket.UploadFromBytes(fileName, data);

            try
            {
                var doc = new UploadDoc
                {
                    Date = DateTime.Now,
                    UserId = "migration",
                    ReferenceId = "",
                    FileName = fileName,
                    FileId = fileId
                };

                var collection = _database.GetCollection<UploadDoc>("UploadDoc");
                collection.InsertOne(doc);
                return (doc.Id, fileId, fileName);
            }
            catch
            {
                // 元数据写入失败时清理已上传的 GridFS 文件，避免产生孤儿文件
                _bucket.Delete(fileId);
                throw;
            }
        }

        /// <summary>
        /// 按 GridFS 文件 id 下载（用于迁移后的校验）
        /// </summary>
        public byte[] DownloadAsBytes(ObjectId fileId)
        {
            return _bucket.DownloadAsBytes(fileId);
        }
    }
}
