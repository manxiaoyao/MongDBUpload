namespace MongDBUpload
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            this.tabMain = new System.Windows.Forms.TabPage();
            this.tabCos = new System.Windows.Forms.TabPage();
            this.tabMongo = new System.Windows.Forms.TabPage();
            this.tabSql = new System.Windows.Forms.TabPage();
            this.tabs = new System.Windows.Forms.TabControl();

            this.txtLog = new System.Windows.Forms.TextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnTest = new System.Windows.Forms.Button();
            this.btnClearLog = new System.Windows.Forms.Button();

            this.chkDownload = new System.Windows.Forms.CheckBox();
            this.chkWriteSql = new System.Windows.Forms.CheckBox();
            this.chkSkipExisting = new System.Windows.Forms.CheckBox();
            this.numMaxCount = new System.Windows.Forms.NumericUpDown();
            this.lblMaxCount = new System.Windows.Forms.Label();
            this.lblMaxCountTip = new System.Windows.Forms.Label();

            // ---------- 主体布局 ----------
            this.tabs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxCount)).BeginInit();
            this.SuspendLayout();

            // tabs
            this.tabs.Controls.Add(this.tabCos);
            this.tabs.Controls.Add(this.tabMongo);
            this.tabs.Controls.Add(this.tabSql);
            this.tabs.Location = new System.Drawing.Point(12, 12);
            this.tabs.Size = new System.Drawing.Size(960, 300);
            this.tabs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            // 各参数页由 BuildCosTab / BuildMongoTab / BuildSqlTab 填充
            BuildCosTab();
            BuildMongoTab();
            BuildSqlTab();

            // ---------- 选项 ----------
            var grpOption = new System.Windows.Forms.GroupBox();
            grpOption.Text = "执行选项";
            grpOption.Location = new System.Drawing.Point(12, 320);
            grpOption.Size = new System.Drawing.Size(960, 88);
            grpOption.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            this.chkDownload.Text = "下载并写入 MongoDB（不勾选则只写关联关系）";
            this.chkDownload.Location = new System.Drawing.Point(12, 24);
            this.chkDownload.AutoSize = true;

            this.chkWriteSql.Text = "写入 SQL Server 关联表";
            this.chkWriteSql.Location = new System.Drawing.Point(330, 24);
            this.chkWriteSql.AutoSize = true;

            this.chkSkipExisting.Text = "跳过已迁移文件";
            this.chkSkipExisting.Location = new System.Drawing.Point(520, 24);
            this.chkSkipExisting.AutoSize = true;

            this.lblMaxCount.Text = "本次最多处理(0=全部)：";
            this.lblMaxCount.Location = new System.Drawing.Point(12, 56);
            this.lblMaxCount.AutoSize = true;
            this.lblMaxCount.Visible = true;

            this.numMaxCount.Location = new System.Drawing.Point(150, 54);
            this.numMaxCount.Size = new System.Drawing.Size(90, 23);
            this.numMaxCount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numMaxCount.ThousandsSeparator = true;
            this.numMaxCount.ValueChanged += new System.EventHandler(this.numMaxCount_ValueChanged);

            this.lblMaxCountTip.Text = "只从腾讯云读取并同步指定数量的文件到 MongoDB，便于小批量试跑。";
            this.lblMaxCountTip.Location = new System.Drawing.Point(250, 58);
            this.lblMaxCountTip.AutoSize = true;
            this.lblMaxCountTip.ForeColor = System.Drawing.Color.Gray;

            grpOption.Controls.Add(this.chkDownload);
            grpOption.Controls.Add(this.chkWriteSql);
            grpOption.Controls.Add(this.chkSkipExisting);
            grpOption.Controls.Add(this.lblMaxCount);
            grpOption.Controls.Add(this.numMaxCount);
            grpOption.Controls.Add(this.lblMaxCountTip);

            // ---------- 按钮 ----------
            this.btnStart.Text = "开始迁移";
            this.btnStart.Location = new System.Drawing.Point(12, 418);
            this.btnStart.Size = new System.Drawing.Size(140, 34);
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(0, 120, 212);
            this.btnStart.ForeColor = System.Drawing.Color.White;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);

            this.btnStop.Text = "停止";
            this.btnStop.Location = new System.Drawing.Point(164, 418);
            this.btnStop.Size = new System.Drawing.Size(100, 34);
            this.btnStop.Enabled = false;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);

            this.btnTest.Text = "测试连接";
            this.btnTest.Location = new System.Drawing.Point(276, 418);
            this.btnTest.Size = new System.Drawing.Size(100, 34);
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);

            this.btnClearLog.Text = "清空日志";
            this.btnClearLog.Location = new System.Drawing.Point(388, 418);
            this.btnClearLog.Size = new System.Drawing.Size(100, 34);
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);

            this.progressBar.Location = new System.Drawing.Point(500, 424);
            this.progressBar.Size = new System.Drawing.Size(472, 22);
            this.progressBar.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            this.lblStatus.Text = "就绪";
            this.lblStatus.Location = new System.Drawing.Point(12, 460);
            this.lblStatus.AutoSize = true;

            this.txtLog.Location = new System.Drawing.Point(12, 482);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtLog.WordWrap = false;
            this.txtLog.ReadOnly = true;
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(30, 30, 30);
            this.txtLog.ForeColor = System.Drawing.Color.Gainsboro;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.Size = new System.Drawing.Size(960, 240);
            this.txtLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;

            // ---------- Form ----------
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 732);
            this.MinimumSize = new System.Drawing.Size(1000, 672);
            this.Controls.Add(this.tabs);
            this.Controls.Add(grpOption);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtLog);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "腾讯云 COS 到 MongoDB 文件迁移工具";
            this.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);

            this.tabs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numMaxCount)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>
        /// 腾讯云参数页
        /// </summary>
        private void BuildCosTab()
        {
            this.tabCos.Text = "腾讯云 COS";
            this.tabCos.UseVisualStyleBackColor = true;

            var y = 20;
            AddLabel(this.tabCos, "存储桶 Bucket：", y);
            this.txtCosBucket = AddText(this.tabCos, 170, y, 300);
            AddLabel(this.tabCos, "地域 Region：", y, 500);
            this.txtCosRegion = AddText(this.tabCos, 620, y, 300);

            y += 34;
            AddLabel(this.tabCos, "APPID：", y);
            this.txtCosAppId = AddText(this.tabCos, 170, y, 300);
            AddLabel(this.tabCos, "前缀 Prefix：", y, 500);
            this.txtCosPrefix = AddText(this.tabCos, 620, y, 300);

            y += 34;
            AddLabel(this.tabCos, "SecretId：", y);
            this.txtCosSecretId = AddText(this.tabCos, 170, y, 620);

            y += 34;
            AddLabel(this.tabCos, "SecretKey：", y);
            this.txtCosSecretKey = AddText(this.tabCos, 170, y, 620);
            this.txtCosSecretKey.UseSystemPasswordChar = true;

            y += 34;
            var btnShow = new System.Windows.Forms.Button();
            btnShow.Text = "显示/隐藏密钥";
            btnShow.Location = new System.Drawing.Point(170, y);
            btnShow.Size = new System.Drawing.Size(130, 26);
            btnShow.Click += (s, e) => { this.txtCosSecretKey.UseSystemPasswordChar = !this.txtCosSecretKey.UseSystemPasswordChar; };
            this.tabCos.Controls.Add(btnShow);
        }

        /// <summary>
        /// MongoDB 参数页
        /// </summary>
        private void BuildMongoTab()
        {
            this.tabMongo.Text = "MongoDB";
            this.tabMongo.UseVisualStyleBackColor = true;

            var y = 20;
            AddLabel(this.tabMongo, "连接字符串：", y);
            this.txtMongoConn = AddText(this.tabMongo, 170, y, 750);

            y += 34;
            AddLabel(this.tabMongo, "数据库名：", y);
            this.txtMongoDb = AddText(this.tabMongo, 170, y, 300);
            AddLabel(this.tabMongo, "GridFS 集合（可空）：", y, 500);
            this.txtGridFs = AddText(this.tabMongo, 700, y, 220);

            y += 34;
            AddLabel(this.tabMongo, "访问地址前缀：", y);
            this.txtMongoUrlPrefix = AddText(this.tabMongo, 170, y, 750);

            y += 44;
            var lbl = new System.Windows.Forms.Label();
            lbl.Text = "说明：文件内容存入 GridFS，同时写入 UploadDoc 集合，ERP 通过「访问地址前缀 + 文档ID」读取文件。";
            lbl.Location = new System.Drawing.Point(20, y);
            lbl.AutoSize = true;
            lbl.ForeColor = System.Drawing.Color.DimGray;
            this.tabMongo.Controls.Add(lbl);
        }

        /// <summary>
        /// SQL Server 关联表参数页
        /// </summary>
        private void BuildSqlTab()
        {
            this.tabSql.Text = "SQL Server 关联表";
            this.tabSql.UseVisualStyleBackColor = true;

            var y = 20;
            AddLabel(this.tabSql, "连接字符串：", y);
            this.txtSqlConn = AddText(this.tabSql, 170, y, 750);

            y += 34;
            AddLabel(this.tabSql, "关联表名：", y);
            this.txtSqlTable = AddText(this.tabSql, 170, y, 300);

            y += 44;
            var lbl = new System.Windows.Forms.Label();
            lbl.Text = "说明：表中一行记录一条映射，一边是腾讯云地址(CosKey/CosUrl)，一边是 MongoDB 地址(MongoDocId/MongoUrl)。" + System.Environment.NewLine +
                       "迁移数据库数据时可用 CosUrl 或 CosKey 关联替换成 MongoUrl。表不存在时自动创建。";
            lbl.Location = new System.Drawing.Point(20, y);
            lbl.AutoSize = true;
            lbl.ForeColor = System.Drawing.Color.DimGray;
            this.tabSql.Controls.Add(lbl);
        }

        private static void AddLabel(System.Windows.Forms.Control parent, string text, int y, int x = 20)
        {
            var lbl = new System.Windows.Forms.Label();
            lbl.Text = text;
            lbl.Location = new System.Drawing.Point(x, y + 4);
            lbl.AutoSize = true;
            parent.Controls.Add(lbl);
        }

        private static System.Windows.Forms.TextBox AddText(System.Windows.Forms.Control parent, int x, int y, int width)
        {
            var txt = new System.Windows.Forms.TextBox();
            txt.Location = new System.Drawing.Point(x, y);
            txt.Size = new System.Drawing.Size(width, 25);
            parent.Controls.Add(txt);
            return txt;
        }

        #endregion

        private System.Windows.Forms.TabControl tabs;
        private System.Windows.Forms.TabPage tabCos;
        private System.Windows.Forms.TabPage tabMongo;
        private System.Windows.Forms.TabPage tabSql;
        private System.Windows.Forms.TabPage tabMain;

        private System.Windows.Forms.TextBox txtCosBucket;
        private System.Windows.Forms.TextBox txtCosRegion;
        private System.Windows.Forms.TextBox txtCosAppId;
        private System.Windows.Forms.TextBox txtCosPrefix;
        private System.Windows.Forms.TextBox txtCosSecretId;
        private System.Windows.Forms.TextBox txtCosSecretKey;

        private System.Windows.Forms.TextBox txtMongoConn;
        private System.Windows.Forms.TextBox txtMongoDb;
        private System.Windows.Forms.TextBox txtGridFs;
        private System.Windows.Forms.TextBox txtMongoUrlPrefix;

        private System.Windows.Forms.TextBox txtSqlConn;
        private System.Windows.Forms.TextBox txtSqlTable;

        private System.Windows.Forms.CheckBox chkDownload;
        private System.Windows.Forms.CheckBox chkWriteSql;
        private System.Windows.Forms.CheckBox chkSkipExisting;
        private System.Windows.Forms.NumericUpDown numMaxCount;
        private System.Windows.Forms.Label lblMaxCount;
        private System.Windows.Forms.Label lblMaxCountTip;

        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.Button btnClearLog;
    }
}
