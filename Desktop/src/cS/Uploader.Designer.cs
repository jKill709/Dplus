namespace Dplus_Desktop
{
    partial class Uploader
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
            SourceFiles_Box = new ListView();
            FileName = new ColumnHeader();
            LastUploaded = new ColumnHeader();
            LastModified = new ColumnHeader();
            IsForHub = new ColumnHeader();
            IsForNode = new ColumnHeader();
            Reboot_Button = new Button();
            Shutdown_Button = new Button();
            StartService_Button = new Button();
            SystemCTLService_GroupBox = new GroupBox();
            CheckServiceStatus_Button = new Button();
            Download_Button = new Button();
            StopService_Button = new Button();
            Upload_Button = new Button();
            ManualRecompile_Button = new Button();
            SourceCode_GroupBox = new GroupBox();
            BackupFirst_Box = new CheckBox();
            AutoRecompile_Button = new Button();
            CreateJSONfiles_Button = new Button();
            DistributeRuntimeFiles_Button = new Button();
            RaspberryPi_GroupBox = new GroupBox();
            Controls_GroupBox = new GroupBox();
            CurrentCluster_StatusStrip = new Dplus_Desktop.WinForms.Controls.ClusterStatusDisplay.DeviceStatusStrip();
            ModelFiles_Box = new ListView();
            ModelName = new ColumnHeader();
            ModelType = new ColumnHeader();
            LastPushed = new ColumnHeader();
            LastModifiedModels = new ColumnHeader();
            Clusters_Box = new ComboBox();
            SourceFiles_Box_Label = new Label();
            ModelFiles_Box_Label = new Label();
            RuntimeFiles_Box_Label = new Label();
            RuntimeFiles_Box = new ListView();
            RuntimeName = new ColumnHeader();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            Nodes_Box_Label = new Label();
            Nodes_Box = new ListView();
            NodeName = new ColumnHeader();
            IPAddress = new ColumnHeader();
            NodeLastUploaded = new ColumnHeader();
            NodeLastModified = new ColumnHeader();
            SystemCTLService_GroupBox.SuspendLayout();
            SourceCode_GroupBox.SuspendLayout();
            RaspberryPi_GroupBox.SuspendLayout();
            Controls_GroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // SourceFiles_Box
            // 
            SourceFiles_Box.Columns.AddRange(new ColumnHeader[] { FileName, LastUploaded, LastModified, IsForHub, IsForNode });
            SourceFiles_Box.FullRowSelect = true;
            SourceFiles_Box.GridLines = true;
            SourceFiles_Box.Location = new Point(12, 56);
            SourceFiles_Box.Name = "SourceFiles_Box";
            SourceFiles_Box.Size = new Size(528, 195);
            SourceFiles_Box.TabIndex = 1;
            SourceFiles_Box.UseCompatibleStateImageBehavior = false;
            SourceFiles_Box.View = View.Details;
            // 
            // FileName
            // 
            FileName.Text = "Name";
            FileName.Width = 156;
            // 
            // LastUploaded
            // 
            LastUploaded.Text = "Last Uploaded";
            LastUploaded.Width = 145;
            // 
            // LastModified
            // 
            LastModified.Text = "Last Modified";
            LastModified.Width = 145;
            // 
            // IsForHub
            // 
            IsForHub.Text = "Hub";
            IsForHub.Width = 30;
            // 
            // IsForNode
            // 
            IsForNode.Text = "Node";
            IsForNode.Width = 30;
            // 
            // Reboot_Button
            // 
            Reboot_Button.Location = new Point(6, 22);
            Reboot_Button.Name = "Reboot_Button";
            Reboot_Button.Size = new Size(75, 23);
            Reboot_Button.TabIndex = 4;
            Reboot_Button.Text = "Reboot";
            Reboot_Button.UseVisualStyleBackColor = true;
            Reboot_Button.Click += Reboot_Button_Click;
            // 
            // Shutdown_Button
            // 
            Shutdown_Button.Location = new Point(6, 51);
            Shutdown_Button.Name = "Shutdown_Button";
            Shutdown_Button.Size = new Size(75, 23);
            Shutdown_Button.TabIndex = 5;
            Shutdown_Button.Text = "Shutdown";
            Shutdown_Button.UseVisualStyleBackColor = true;
            Shutdown_Button.Click += Shutdown_Button_Click;
            // 
            // StartService_Button
            // 
            StartService_Button.Location = new Point(6, 22);
            StartService_Button.Name = "StartService_Button";
            StartService_Button.Size = new Size(75, 23);
            StartService_Button.TabIndex = 7;
            StartService_Button.Text = "Start";
            StartService_Button.UseVisualStyleBackColor = true;
            StartService_Button.Click += RunMain_Button_Click;
            // 
            // SystemCTLService_GroupBox
            // 
            SystemCTLService_GroupBox.Controls.Add(CheckServiceStatus_Button);
            SystemCTLService_GroupBox.Controls.Add(Download_Button);
            SystemCTLService_GroupBox.Controls.Add(StopService_Button);
            SystemCTLService_GroupBox.Controls.Add(StartService_Button);
            SystemCTLService_GroupBox.Location = new Point(354, 22);
            SystemCTLService_GroupBox.Name = "SystemCTLService_GroupBox";
            SystemCTLService_GroupBox.Size = new Size(168, 80);
            SystemCTLService_GroupBox.TabIndex = 9;
            SystemCTLService_GroupBox.TabStop = false;
            SystemCTLService_GroupBox.Text = "SystemCTL Service";
            // 
            // CheckServiceStatus_Button
            // 
            CheckServiceStatus_Button.Location = new Point(87, 51);
            CheckServiceStatus_Button.Name = "CheckServiceStatus_Button";
            CheckServiceStatus_Button.Size = new Size(75, 23);
            CheckServiceStatus_Button.TabIndex = 9;
            CheckServiceStatus_Button.Text = "Status";
            CheckServiceStatus_Button.UseVisualStyleBackColor = true;
            CheckServiceStatus_Button.Click += CheckServiceStatus_Button_Click;
            // 
            // Download_Button
            // 
            Download_Button.Location = new Point(6, 51);
            Download_Button.Name = "Download_Button";
            Download_Button.Size = new Size(75, 23);
            Download_Button.TabIndex = 9;
            Download_Button.Text = "Download";
            Download_Button.UseVisualStyleBackColor = true;
            Download_Button.Click += DownloadFiles_Button_Click;
            // 
            // StopService_Button
            // 
            StopService_Button.Location = new Point(87, 22);
            StopService_Button.Name = "StopService_Button";
            StopService_Button.Size = new Size(75, 23);
            StopService_Button.TabIndex = 8;
            StopService_Button.Text = "Stop";
            StopService_Button.UseVisualStyleBackColor = true;
            StopService_Button.Click += StopService_Button_Click;
            // 
            // Upload_Button
            // 
            Upload_Button.Location = new Point(6, 22);
            Upload_Button.Name = "Upload_Button";
            Upload_Button.Size = new Size(75, 23);
            Upload_Button.TabIndex = 0;
            Upload_Button.Text = "UL Code";
            Upload_Button.UseVisualStyleBackColor = true;
            Upload_Button.Click += Upload_Button_Click;
            // 
            // ManualRecompile_Button
            // 
            ManualRecompile_Button.Location = new Point(87, 22);
            ManualRecompile_Button.Name = "ManualRecompile_Button";
            ManualRecompile_Button.Size = new Size(75, 23);
            ManualRecompile_Button.TabIndex = 8;
            ManualRecompile_Button.Text = "Manual";
            ManualRecompile_Button.UseVisualStyleBackColor = true;
            ManualRecompile_Button.Click += ManualRecompile_Button_Click;
            // 
            // SourceCode_GroupBox
            // 
            SourceCode_GroupBox.Controls.Add(BackupFirst_Box);
            SourceCode_GroupBox.Controls.Add(AutoRecompile_Button);
            SourceCode_GroupBox.Controls.Add(CreateJSONfiles_Button);
            SourceCode_GroupBox.Controls.Add(DistributeRuntimeFiles_Button);
            SourceCode_GroupBox.Controls.Add(ManualRecompile_Button);
            SourceCode_GroupBox.Controls.Add(Upload_Button);
            SourceCode_GroupBox.Location = new Point(99, 22);
            SourceCode_GroupBox.Name = "SourceCode_GroupBox";
            SourceCode_GroupBox.Size = new Size(249, 80);
            SourceCode_GroupBox.TabIndex = 10;
            SourceCode_GroupBox.TabStop = false;
            SourceCode_GroupBox.Text = "Source Code";
            // 
            // BackupFirst_Box
            // 
            BackupFirst_Box.AutoSize = true;
            BackupFirst_Box.Location = new Point(168, 51);
            BackupFirst_Box.Name = "BackupFirst_Box";
            BackupFirst_Box.Size = new Size(65, 19);
            BackupFirst_Box.TabIndex = 13;
            BackupFirst_Box.Text = "Backup";
            BackupFirst_Box.UseVisualStyleBackColor = true;
            // 
            // AutoRecompile_Button
            // 
            AutoRecompile_Button.Location = new Point(168, 22);
            AutoRecompile_Button.Name = "AutoRecompile_Button";
            AutoRecompile_Button.Size = new Size(75, 23);
            AutoRecompile_Button.TabIndex = 12;
            AutoRecompile_Button.Text = "Auto";
            AutoRecompile_Button.UseVisualStyleBackColor = true;
            AutoRecompile_Button.Click += AutoRecompile_Button1_Click;
            // 
            // CreateJSONfiles_Button
            // 
            CreateJSONfiles_Button.Location = new Point(6, 51);
            CreateJSONfiles_Button.Name = "CreateJSONfiles_Button";
            CreateJSONfiles_Button.Size = new Size(75, 23);
            CreateJSONfiles_Button.TabIndex = 11;
            CreateJSONfiles_Button.Text = "Create json";
            CreateJSONfiles_Button.UseVisualStyleBackColor = true;
            CreateJSONfiles_Button.Click += CreateSettingsFiles_Button_Click;
            // 
            // DistributeRuntimeFiles_Button
            // 
            DistributeRuntimeFiles_Button.Location = new Point(87, 51);
            DistributeRuntimeFiles_Button.Name = "DistributeRuntimeFiles_Button";
            DistributeRuntimeFiles_Button.Size = new Size(75, 23);
            DistributeRuntimeFiles_Button.TabIndex = 10;
            DistributeRuntimeFiles_Button.Text = "Distribute";
            DistributeRuntimeFiles_Button.UseVisualStyleBackColor = true;
            DistributeRuntimeFiles_Button.Click += DistributeRuntimeFiles_Button_Click;
            // 
            // RaspberryPi_GroupBox
            // 
            RaspberryPi_GroupBox.Controls.Add(Shutdown_Button);
            RaspberryPi_GroupBox.Controls.Add(Reboot_Button);
            RaspberryPi_GroupBox.Location = new Point(6, 22);
            RaspberryPi_GroupBox.Name = "RaspberryPi_GroupBox";
            RaspberryPi_GroupBox.Size = new Size(87, 80);
            RaspberryPi_GroupBox.TabIndex = 11;
            RaspberryPi_GroupBox.TabStop = false;
            RaspberryPi_GroupBox.Text = "Raspberry Pi ";
            // 
            // Controls_GroupBox
            // 
            Controls_GroupBox.Controls.Add(SourceCode_GroupBox);
            Controls_GroupBox.Controls.Add(SystemCTLService_GroupBox);
            Controls_GroupBox.Controls.Add(RaspberryPi_GroupBox);
            Controls_GroupBox.Location = new Point(12, 661);
            Controls_GroupBox.Name = "Controls_GroupBox";
            Controls_GroupBox.Size = new Size(528, 110);
            Controls_GroupBox.TabIndex = 13;
            Controls_GroupBox.TabStop = false;
            Controls_GroupBox.Text = "Controls";
            // 
            // CurrentCluster_StatusStrip
            // 
            CurrentCluster_StatusStrip.Font = new Font("Segoe UI", 8F);
            CurrentCluster_StatusStrip.Location = new Point(198, 4);
            CurrentCluster_StatusStrip.MinimumSize = new Size(150, 25);
            CurrentCluster_StatusStrip.Name = "CurrentCluster_StatusStrip";
            CurrentCluster_StatusStrip.Size = new Size(342, 31);
            CurrentCluster_StatusStrip.TabIndex = 18;
            CurrentCluster_StatusStrip.DoubleClick += CheckServiceStatus_Button_Click;
            // 
            // ModelFiles_Box
            // 
            ModelFiles_Box.Columns.AddRange(new ColumnHeader[] { ModelName, ModelType, LastPushed, LastModifiedModels });
            ModelFiles_Box.FullRowSelect = true;
            ModelFiles_Box.GridLines = true;
            ModelFiles_Box.Location = new Point(12, 398);
            ModelFiles_Box.Name = "ModelFiles_Box";
            ModelFiles_Box.Size = new Size(528, 112);
            ModelFiles_Box.TabIndex = 14;
            ModelFiles_Box.UseCompatibleStateImageBehavior = false;
            ModelFiles_Box.View = View.Details;
            // 
            // ModelName
            // 
            ModelName.Text = "Model Name";
            ModelName.Width = 126;
            // 
            // ModelType
            // 
            ModelType.Text = "Model Type";
            ModelType.Width = 90;
            // 
            // LastPushed
            // 
            LastPushed.Text = "LastPushed";
            LastPushed.Width = 145;
            // 
            // LastModifiedModels
            // 
            LastModifiedModels.Text = "Last Modified";
            LastModifiedModels.Width = 145;
            // 
            // Clusters_Box
            // 
            Clusters_Box.FormattingEnabled = true;
            Clusters_Box.Location = new Point(12, 12);
            Clusters_Box.Name = "Clusters_Box";
            Clusters_Box.Size = new Size(197, 23);
            Clusters_Box.TabIndex = 15;
            Clusters_Box.SelectedIndexChanged += Clusters_Box_SelectedIndexChanged;
            // 
            // SourceFiles_Box_Label
            // 
            SourceFiles_Box_Label.AutoSize = true;
            SourceFiles_Box_Label.Location = new Point(12, 38);
            SourceFiles_Box_Label.Name = "SourceFiles_Box_Label";
            SourceFiles_Box_Label.Size = new Size(30, 15);
            SourceFiles_Box_Label.TabIndex = 16;
            SourceFiles_Box_Label.Text = "Files";
            // 
            // ModelFiles_Box_Label
            // 
            ModelFiles_Box_Label.AutoSize = true;
            ModelFiles_Box_Label.Location = new Point(12, 380);
            ModelFiles_Box_Label.Name = "ModelFiles_Box_Label";
            ModelFiles_Box_Label.Size = new Size(46, 15);
            ModelFiles_Box_Label.TabIndex = 18;
            ModelFiles_Box_Label.Text = "Models";
            // 
            // RuntimeFiles_Box_Label
            // 
            RuntimeFiles_Box_Label.AutoSize = true;
            RuntimeFiles_Box_Label.Location = new Point(12, 254);
            RuntimeFiles_Box_Label.Name = "RuntimeFiles_Box_Label";
            RuntimeFiles_Box_Label.Size = new Size(78, 15);
            RuntimeFiles_Box_Label.TabIndex = 19;
            RuntimeFiles_Box_Label.Text = "Runtime Files";
            // 
            // RuntimeFiles_Box
            // 
            RuntimeFiles_Box.Columns.AddRange(new ColumnHeader[] { RuntimeName, columnHeader1, columnHeader2, columnHeader3 });
            RuntimeFiles_Box.FullRowSelect = true;
            RuntimeFiles_Box.GridLines = true;
            RuntimeFiles_Box.Location = new Point(12, 272);
            RuntimeFiles_Box.Name = "RuntimeFiles_Box";
            RuntimeFiles_Box.Size = new Size(528, 105);
            RuntimeFiles_Box.TabIndex = 20;
            RuntimeFiles_Box.UseCompatibleStateImageBehavior = false;
            RuntimeFiles_Box.View = View.Details;
            // 
            // RuntimeName
            // 
            RuntimeName.Text = "Name";
            RuntimeName.Width = 152;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Last Source Change";
            columnHeader1.Width = 124;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Last Compliled";
            columnHeader2.Width = 124;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Last Pushed";
            columnHeader3.Width = 124;
            // 
            // Nodes_Box_Label
            // 
            Nodes_Box_Label.AutoSize = true;
            Nodes_Box_Label.Location = new Point(12, 513);
            Nodes_Box_Label.Name = "Nodes_Box_Label";
            Nodes_Box_Label.Size = new Size(41, 15);
            Nodes_Box_Label.TabIndex = 22;
            Nodes_Box_Label.Text = "Nodes";
            // 
            // Nodes_Box
            // 
            Nodes_Box.Columns.AddRange(new ColumnHeader[] { NodeName, IPAddress, NodeLastUploaded, NodeLastModified });
            Nodes_Box.FullRowSelect = true;
            Nodes_Box.GridLines = true;
            Nodes_Box.Location = new Point(12, 531);
            Nodes_Box.Name = "Nodes_Box";
            Nodes_Box.Size = new Size(528, 124);
            Nodes_Box.TabIndex = 21;
            Nodes_Box.UseCompatibleStateImageBehavior = false;
            Nodes_Box.View = View.Details;
            // 
            // NodeName
            // 
            NodeName.Text = "Node Name";
            NodeName.Width = 119;
            // 
            // IPAddress
            // 
            IPAddress.Text = "IP";
            IPAddress.Width = 115;
            // 
            // NodeLastUploaded
            // 
            NodeLastUploaded.Text = "Last Uploaded";
            NodeLastUploaded.Width = 145;
            // 
            // NodeLastModified
            // 
            NodeLastModified.Text = "Last Modified";
            NodeLastModified.Width = 145;
            // 
            // Uploader
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(551, 782);
            Controls.Add(Nodes_Box_Label);
            Controls.Add(Nodes_Box);
            Controls.Add(RuntimeFiles_Box);
            Controls.Add(RuntimeFiles_Box_Label);
            Controls.Add(ModelFiles_Box_Label);
            Controls.Add(SourceFiles_Box_Label);
            Controls.Add(Clusters_Box);
            Controls.Add(ModelFiles_Box);
            Controls.Add(Controls_GroupBox);
            Controls.Add(SourceFiles_Box);
            Controls.Add(CurrentCluster_StatusStrip);
            Name = "Uploader";
            Text = "Upload files to camcpp";
            FormClosing += Uploader_FormClosing;
            Load += Uploader_Load;
            SystemCTLService_GroupBox.ResumeLayout(false);
            SourceCode_GroupBox.ResumeLayout(false);
            SourceCode_GroupBox.PerformLayout();
            RaspberryPi_GroupBox.ResumeLayout(false);
            Controls_GroupBox.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ListView SourceFiles_Box;
        private Button Reboot_Button;
        private Button Shutdown_Button;
        private ColumnHeader FileName;
        private ColumnHeader LastUploaded;
        private ColumnHeader LastModified;
        private Button StartService_Button;
        private GroupBox SystemCTLService_GroupBox;
        private Button StopService_Button;
        private Button CheckServiceStatus_Button;
        private Button Upload_Button;
        private Button ManualRecompile_Button;
        private GroupBox SourceCode_GroupBox;
        private GroupBox RaspberryPi_GroupBox;
        private GroupBox Controls_GroupBox;
        private Button Download_Button;
        private ListView ModelFiles_Box;
        private ColumnHeader ModelName;
        private ColumnHeader ModelType;
        private ColumnHeader Device0;
        private ColumnHeader Device1;
        private ColumnHeader Device2;
        private ColumnHeader Device3;
        private ColumnHeader Device4;
        private Button DistributeRuntimeFiles_Button;
        private ComboBox Clusters_Box;
        private Label SourceFiles_Box_Label;
        private Label ModelFiles_Box_Label;
        private Label RuntimeFiles_Box_Label;
        private ListView RuntimeFiles_Box;
        private ColumnHeader RuntimeName;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader2;
        private Label Nodes_Box_Label;
        private ListView Nodes_Box;
        private ColumnHeader NodeName;
        private ColumnHeader NodeLastUploaded;
        private ColumnHeader NodeLastModified;
        private ColumnHeader IsForHub;
        private ColumnHeader IsForNode;
        private ColumnHeader LastPushed;
        private ColumnHeader LastModifiedModels;
        private ColumnHeader IPAddress;
        private ColumnHeader columnHeader1;
        private Button CreateJSONfiles_Button;
        private Button AutoRecompile_Button;
        private CheckBox BackupFirst_Box;
        private Dplus_Desktop.WinForms.Controls.ClusterStatusDisplay.DeviceStatusStrip CurrentCluster_StatusStrip;
    }
}
