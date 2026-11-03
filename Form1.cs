using System.Text;

namespace MongDBUpload
{
    public partial class Form1 : Form
    {
        private readonly MigrationOptions _opt = new MigrationOptions();
        private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        private CancellationTokenSource? _cts;
        private bool _running;

        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
            // 关闭窗口时把界面上的配置保存回本地配置文件，下次启动自动回填
            FormClosing += (s, e) =>
            {
                try { CollectOptions().SaveToFile(_configPath); } catch { }
            };
        }

        private void Form1_Load(object? sender, EventArgs e)
        {
            // 启动时从本地配置文件读取参数；文件损坏时用默认值并提示
            try
            {
                _opt.LoadFromFile(_configPath);
            }
            catch (Exception ex)
            {
                Log("读取配置文件失败，本次使用默认值：" + ex.Message);
            }

            // 用读取到的参数预填界面
            txtCosBucket.Text = _opt.CosBucket;
            txtCosRegion.Text = _opt.CosRegion;
            txtCosAppId.Text = _opt.CosAppId;
            txtCosPrefix.Text = _opt.CosPrefix;
            txtCosSecretId.Text = _opt.CosSecretId;
            txtCosSecretKey.Text = _opt.CosSecretKey;

            txtMongoConn.Text = _opt.MongoConnectionString;
            txtMongoDb.Text = _opt.MongoDatabaseName;
            txtGridFs.Text = _opt.GridFsBucketName;
            txtMongoUrlPrefix.Text = _opt.MongoUrlPrefix;

            txtSqlConn.Text = _opt.SqlConnectionString;
            txtSqlTable.Text = _opt.MappingTableName;

            chkDownload.Checked = _opt.DownloadFileContent;
            chkWriteSql.Checked = _opt.WriteSqlMapping;
            chkSkipExisting.Checked = _opt.SkipExisting;

            numMaxCount.Minimum = 0;
            numMaxCount.Maximum = 1000000;
            numMaxCount.Value = _opt.MaxCount;
            UpdateMaxCountTip();

            // 首次运行：生成默认配置文件，方便用户直接用文本编辑器填写
            if (!File.Exists(_configPath))
            {
                try
                {
                    _opt.SaveToFile(_configPath);
                    Log($"已生成配置文件（默认值，可直接编辑）：{_configPath}");
                }
                catch (Exception ex)
                {
                    Log("生成配置文件失败：" + ex.Message);
                }
            }
            Log($"配置文件：{_configPath}（含密钥，仅保存在本机，不入库）");

            Log("准备就绪。请确认各页参数后点击“开始迁移”。");
            Log($"腾讯云存储桶：{txtCosBucket.Text}，地域：{txtCosRegion.Text}");
            Log($"MongoDB 数据库：{txtMongoDb.Text}，关联表：{txtSqlTable.Text}");
        }

        /// <summary>
        /// 数量输入框变化时刷新提示文字，让 0 的含义一目了然
        /// </summary>
        private void numMaxCount_ValueChanged(object? sender, EventArgs e)
        {
            UpdateMaxCountTip();
        }

        private void UpdateMaxCountTip()
        {
            int v = (int)numMaxCount.Value;
            lblMaxCountTip.Text = v <= 0
                ? "0 = 处理全部文件。"
                : $"本次只同步 {v} 个文件，其余文件下次运行再处理。";
            lblMaxCountTip.ForeColor = v <= 0
                ? System.Drawing.Color.Gray
                : System.Drawing.Color.FromArgb(0, 120, 212);
        }

        /// <summary>
        /// 把界面参数收集到 MigrationOptions
        /// </summary>
        private MigrationOptions CollectOptions()
        {
            _opt.CosBucket = txtCosBucket.Text.Trim();
            _opt.CosRegion = txtCosRegion.Text.Trim();
            _opt.CosAppId = txtCosAppId.Text.Trim();
            _opt.CosPrefix = txtCosPrefix.Text.Trim();
            _opt.CosSecretId = txtCosSecretId.Text.Trim();
            _opt.CosSecretKey = txtCosSecretKey.Text.Trim();

            _opt.MongoConnectionString = txtMongoConn.Text.Trim();
            _opt.MongoDatabaseName = txtMongoDb.Text.Trim();
            _opt.GridFsBucketName = txtGridFs.Text.Trim();
            _opt.MongoUrlPrefix = txtMongoUrlPrefix.Text.Trim();

            _opt.SqlConnectionString = txtSqlConn.Text.Trim();
            _opt.MappingTableName = txtSqlTable.Text.Trim();

            _opt.DownloadFileContent = chkDownload.Checked;
            _opt.WriteSqlMapping = chkWriteSql.Checked;
            _opt.SkipExisting = chkSkipExisting.Checked;

            // 0 = 全部，>0 = 本次最多同步多少个文件到 MongoDB
            _opt.MaxCount = (int)numMaxCount.Value;

            return _opt;
        }

        /// <summary>
        /// 把当前界面参数保存到本地配置文件（失败不阻断操作，只记日志）
        /// </summary>
        private void SaveConfig()
        {
            try
            {
                _opt.SaveToFile(_configPath);
            }
            catch (Exception ex)
            {
                Log("保存配置文件失败：" + ex.Message);
            }
        }

        private void ValidateOptions(MigrationOptions opt)
        {
            if (string.IsNullOrWhiteSpace(opt.CosBucket)) throw new Exception("请填写腾讯云存储桶 Bucket。");
            if (string.IsNullOrWhiteSpace(opt.CosRegion)) throw new Exception("请填写腾讯云地域 Region。");
            if (string.IsNullOrWhiteSpace(opt.CosSecretId)) throw new Exception("请填写腾讯云 SecretId。");
            if (string.IsNullOrWhiteSpace(opt.CosSecretKey)) throw new Exception("请填写腾讯云 SecretKey。");
            if (string.IsNullOrWhiteSpace(opt.MongoConnectionString)) throw new Exception("请填写 MongoDB 连接字符串。");
            if (opt.WriteSqlMapping && string.IsNullOrWhiteSpace(opt.SqlConnectionString))
                throw new Exception("已选择写入关联表，但未填写 SQL Server 连接字符串。");
            if (!opt.DownloadFileContent && !opt.WriteSqlMapping)
                throw new Exception("“写入 MongoDB”和“写入关联表”至少要选择一个。");
        }

        private async void btnStart_Click(object? sender, EventArgs e)
        {
            if (_running) return;

            MigrationOptions opt;
            try
            {
                opt = CollectOptions();
                ValidateOptions(opt);
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "参数校验", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _cts = new CancellationTokenSource();
            SetRunning(true);
            progressBar.Value = 0;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await Task.Run(() =>
                {
                    var service = new MigrationService(opt, Log, UpdateProgress);
                    return service.Run(_cts.Token);
                }, _cts.Token);

                Log($"全部结束，耗时 {result.Elapsed:hh\\:mm\\:ss}。");
                SetStatus($"完成：成功 {result.Success}，跳过 {result.Skipped}，失败 {result.Failed}");
            }
            catch (OperationCanceledException)
            {
                Log("用户已取消迁移。");
                SetStatus("已取消");
            }
            catch (Exception ex)
            {
                Log("发生错误：" + ex.Message);
                SetStatus("出错");
                MessageBox.Show(ex.Message, "迁移失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetRunning(false);
            }
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
            if (!_running) return;
            btnStop.Enabled = false;
            Log("正在停止，请等待当前文件处理完成 ...");
            _cts?.Cancel();
        }

        private async void btnTest_Click(object? sender, EventArgs e)
        {
            if (_running) return;

            MigrationOptions opt;
            try
            {
                opt = CollectOptions();
                SaveConfig();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "参数校验", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTest.Enabled = false;
            SetStatus("正在测试连接 ...");
            Log("========== 开始测试连接 ==========");

            await Task.Run(() =>
            {
                // 腾讯云
                try
                {
                    var cos = new CosStorage(opt);
                    var list = cos.ListAll(opt.CosPrefix, null, CancellationToken.None, 5);
                    Log($"[OK] 腾讯云 COS 连接成功，前 5 个文件：");
                    foreach (var item in list)
                    {
                        Log($"      {item.Key}（{item.Size} 字节）");
                    }
                }
                catch (Exception ex)
                {
                    Log("[失败] 腾讯云 COS：" + ex.Message);
                }

                // MongoDB
                try
                {
                    var mongo = new MongoFileStorage(opt);
                    mongo.Ping();
                    Log($"[OK] MongoDB 连接成功，数据库：{opt.MongoDatabaseName}。");
                }
                catch (Exception ex)
                {
                    Log("[失败] MongoDB：" + ex.Message);
                }

                // SQL Server
                try
                {
                    if (string.IsNullOrWhiteSpace(opt.SqlConnectionString))
                    {
                        Log("[跳过] SQL Server：未填写连接字符串。");
                    }
                    else
                    {
                        var sql = new SqlMappingStore(opt.SqlConnectionString, opt.MappingTableName);
                        sql.Ping();
                        sql.EnsureTable();
                        Log($"[OK] SQL Server 连接成功，关联表 dbo.{sql.TableName} 已就绪。");
                    }
                }
                catch (Exception ex)
                {
                    Log("[失败] SQL Server：" + ex.Message);
                }
            });

            Log("========== 测试结束 ==========");
            SetStatus("测试完成");
            btnTest.Enabled = true;
        }

        private void btnClearLog_Click(object? sender, EventArgs e)
        {
            txtLog.Clear();
        }

        /// <summary>
        /// 日志输出（跨线程安全）
        /// </summary>
        private void Log(string message)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(Log), message); } catch { }
                return;
            }

            var sb = new StringBuilder();
            sb.Append(DateTime.Now.ToString("HH:mm:ss")).Append("  ").AppendLine(message);
            txtLog.AppendText(sb.ToString());

            if (txtLog.TextLength > 4000000)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - 2000000);
            }
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        /// <summary>
        /// 进度更新（跨线程安全）
        /// </summary>
        private void UpdateProgress(int current, int total)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<int, int>(UpdateProgress), current, total); } catch { }
                return;
            }

            if (total <= 0)
            {
                progressBar.Value = 0;
                SetStatus("就绪");
                return;
            }

            int value = (int)Math.Min(100, Math.Round(current * 100.0 / total));
            progressBar.Value = Math.Max(0, Math.Min(100, value));
            SetStatus($"进度 {current}/{total}（{value}%）");
        }

        private void SetStatus(string text)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(SetStatus), text); } catch { }
                return;
            }
            lblStatus.Text = text;
        }

        private void SetRunning(bool running)
        {
            _running = running;
            btnStart.Enabled = !running;
            btnTest.Enabled = !running;
            btnStop.Enabled = running;
            tabs.Enabled = !running;

            if (!running)
            {
                _cts?.Dispose();
                _cts = null;
            }
        }
    }
}
