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
            RecheckStatus_Button = new Button();
            Reboot_Button = new Button();
            Shutdown_Button = new Button();
            Connect = new Button();
            StartService_Button = new Button();
            SystemCTLService_GroupBox = new GroupBox();
            CheckServiceStatus_Button = new Button();
            Download_Button = new Button();
            StopService_Button = new Button();
            Upload_Button = new Button();
            ManualRecompile_Button = new Button();
            SourceCode_GroupBox = new GroupBox();
            AutoRecompile_Button1 = new Button();
            CreateJSONfiles_Button = new Button();
            DistributeRuntimeFiles_Button = new Button();
            RaspberryPi_GroupBox = new GroupBox();
            SSHSCP_GroupBox = new GroupBox();
            Controls_GroupBox = new GroupBox();
            Status_Connction_Label = new Label();
            Status_SourceCode_Label = new Label();
            Status_RaspberryPi_Label = new Label();
            Status_SystemCTLService_Label = new Label();
            Status_SSHSCP_Label = new Label();
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
            columnHeader5 = new ColumnHeader();
            columnHeader6 = new ColumnHeader();
            SystemCTLService_GroupBox.SuspendLayout();
            SourceCode_GroupBox.SuspendLayout();
            RaspberryPi_GroupBox.SuspendLayout();
            SSHSCP_GroupBox.SuspendLayout();
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
            SourceFiles_Box.Size = new Size(764, 195);
            SourceFiles_Box.TabIndex = 1;
            SourceFiles_Box.UseCompatibleStateImageBehavior = false;
            SourceFiles_Box.View = View.Details;
            // 
            // FileName
            // 
            FileName.Text = "Name";
            FileName.Width = 279;
            // 
            // LastUploaded
            // 
            LastUploaded.Text = "Last Uploaded";
            LastUploaded.Width = 175;
            // 
            // LastModified
            // 
            LastModified.Text = "Last Modified";
            LastModified.Width = 175;
            // 
            // IsForHub
            // 
            IsForHub.Text = "Hub";
            IsForHub.Width = 55;
            // 
            // IsForNode
            // 
            IsForNode.Text = "Node";
            IsForNode.Width = 55;
            // 
            // RecheckStatus_Button
            // 
            RecheckStatus_Button.Location = new Point(6, 51);
            RecheckStatus_Button.Name = "RecheckStatus_Button";
            RecheckStatus_Button.Size = new Size(75, 23);
            RecheckStatus_Button.TabIndex = 2;
            RecheckStatus_Button.Text = "Recheck";
            RecheckStatus_Button.UseVisualStyleBackColor = true;
            RecheckStatus_Button.Click += Reselect_Button_Click;
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
            // Connect
            // 
            Connect.Location = new Point(6, 22);
            Connect.Name = "Connect";
            Connect.Size = new Size(75, 23);
            Connect.TabIndex = 6;
            Connect.Text = "Connect";
            Connect.UseVisualStyleBackColor = true;
            Connect.Click += Connect_Click;
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
            SystemCTLService_GroupBox.Location = new Point(447, 22);
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
            SourceCode_GroupBox.Controls.Add(AutoRecompile_Button1);
            SourceCode_GroupBox.Controls.Add(CreateJSONfiles_Button);
            SourceCode_GroupBox.Controls.Add(DistributeRuntimeFiles_Button);
            SourceCode_GroupBox.Controls.Add(ManualRecompile_Button);
            SourceCode_GroupBox.Controls.Add(Upload_Button);
            SourceCode_GroupBox.Location = new Point(192, 22);
            SourceCode_GroupBox.Name = "SourceCode_GroupBox";
            SourceCode_GroupBox.Size = new Size(249, 80);
            SourceCode_GroupBox.TabIndex = 10;
            SourceCode_GroupBox.TabStop = false;
            SourceCode_GroupBox.Text = "Source Code";
            // 
            // AutoRecompile_Button1
            // 
            AutoRecompile_Button1.Location = new Point(168, 22);
            AutoRecompile_Button1.Name = "AutoRecompile_Button1";
            AutoRecompile_Button1.Size = new Size(75, 23);
            AutoRecompile_Button1.TabIndex = 12;
            AutoRecompile_Button1.Text = "Auto";
            AutoRecompile_Button1.UseVisualStyleBackColor = true;
            AutoRecompile_Button1.Click += AutoRecompile_Button1_Click;
            // 
            // CreateJSONfiles_Button
            // 
            CreateJSONfiles_Button.Location = new Point(6, 51);
            CreateJSONfiles_Button.Name = "CreateJSONfiles_Button";
            CreateJSONfiles_Button.Size = new Size(75, 23);
            CreateJSONfiles_Button.TabIndex = 11;
            CreateJSONfiles_Button.Text = "Create json";
            CreateJSONfiles_Button.UseVisualStyleBackColor = true;
            CreateJSONfiles_Button.Click += CreateSettingsfiles_Button_Click;
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
            RaspberryPi_GroupBox.Location = new Point(99, 22);
            RaspberryPi_GroupBox.Name = "RaspberryPi_GroupBox";
            RaspberryPi_GroupBox.Size = new Size(87, 80);
            RaspberryPi_GroupBox.TabIndex = 11;
            RaspberryPi_GroupBox.TabStop = false;
            RaspberryPi_GroupBox.Text = "Raspberry Pi ";
            // 
            // SSHSCP_GroupBox
            // 
            SSHSCP_GroupBox.Controls.Add(RecheckStatus_Button);
            SSHSCP_GroupBox.Controls.Add(Connect);
            SSHSCP_GroupBox.Location = new Point(6, 22);
            SSHSCP_GroupBox.Name = "SSHSCP_GroupBox";
            SSHSCP_GroupBox.Size = new Size(87, 80);
            SSHSCP_GroupBox.TabIndex = 12;
            SSHSCP_GroupBox.TabStop = false;
            SSHSCP_GroupBox.Text = "SSH/SCP";
            // 
            // Controls_GroupBox
            // 
            Controls_GroupBox.Controls.Add(Status_Connction_Label);
            Controls_GroupBox.Controls.Add(Status_SourceCode_Label);
            Controls_GroupBox.Controls.Add(SourceCode_GroupBox);
            Controls_GroupBox.Controls.Add(Status_RaspberryPi_Label);
            Controls_GroupBox.Controls.Add(Status_SystemCTLService_Label);
            Controls_GroupBox.Controls.Add(Status_SSHSCP_Label);
            Controls_GroupBox.Controls.Add(SystemCTLService_GroupBox);
            Controls_GroupBox.Controls.Add(RaspberryPi_GroupBox);
            Controls_GroupBox.Controls.Add(SSHSCP_GroupBox);
            Controls_GroupBox.Location = new Point(12, 661);
            Controls_GroupBox.Name = "Controls_GroupBox";
            Controls_GroupBox.Size = new Size(764, 133);
            Controls_GroupBox.TabIndex = 13;
            Controls_GroupBox.TabStop = false;
            Controls_GroupBox.Text = "Controls";
            // 
            // Status_Connction_Label
            // 
            Status_Connction_Label.AutoSize = true;
            Status_Connction_Label.Location = new Point(537, 105);
            Status_Connction_Label.Margin = new Padding(0);
            Status_Connction_Label.Name = "Status_Connction_Label";
            Status_Connction_Label.Size = new Size(216, 15);
            Status_Connction_Label.TabIndex = 17;
            Status_Connction_Label.Text = "------------------<[?]>------------------";
            // 
            // Status_SourceCode_Label
            // 
            Status_SourceCode_Label.AutoSize = true;
            Status_SourceCode_Label.Location = new Point(186, 105);
            Status_SourceCode_Label.Name = "Status_SourceCode_Label";
            Status_SourceCode_Label.Size = new Size(179, 15);
            Status_SourceCode_Label.TabIndex = 16;
            Status_SourceCode_Label.Text = "--------------<[+]>--------------";
            // 
            // Status_RaspberryPi_Label
            // 
            Status_RaspberryPi_Label.AutoSize = true;
            Status_RaspberryPi_Label.Location = new Point(97, 105);
            Status_RaspberryPi_Label.Name = "Status_RaspberryPi_Label";
            Status_RaspberryPi_Label.Size = new Size(89, 15);
            Status_RaspberryPi_Label.TabIndex = 14;
            Status_RaspberryPi_Label.Text = "-----<[+]>-----";
            // 
            // Status_SystemCTLService_Label
            // 
            Status_SystemCTLService_Label.AutoSize = true;
            Status_SystemCTLService_Label.Location = new Point(360, 105);
            Status_SystemCTLService_Label.Name = "Status_SystemCTLService_Label";
            Status_SystemCTLService_Label.Size = new Size(179, 15);
            Status_SystemCTLService_Label.TabIndex = 15;
            Status_SystemCTLService_Label.Text = "--------------<[+]>--------------";
            // 
            // Status_SSHSCP_Label
            // 
            Status_SSHSCP_Label.AutoSize = true;
            Status_SSHSCP_Label.Location = new Point(7, 105);
            Status_SSHSCP_Label.Name = "Status_SSHSCP_Label";
            Status_SSHSCP_Label.Size = new Size(89, 15);
            Status_SSHSCP_Label.TabIndex = 13;
            Status_SSHSCP_Label.Text = "-----<[+]>-----";
            // 
            // ModelFiles_Box
            // 
            ModelFiles_Box.Columns.AddRange(new ColumnHeader[] { ModelName, ModelType, LastPushed, LastModifiedModels });
            ModelFiles_Box.FullRowSelect = true;
            ModelFiles_Box.GridLines = true;
            ModelFiles_Box.Location = new Point(12, 398);
            ModelFiles_Box.Name = "ModelFiles_Box";
            ModelFiles_Box.Size = new Size(764, 112);
            ModelFiles_Box.TabIndex = 14;
            ModelFiles_Box.UseCompatibleStateImageBehavior = false;
            ModelFiles_Box.View = View.Details;
            // 
            // ModelName
            // 
            ModelName.Text = "Model Name";
            ModelName.Width = 279;
            // 
            // ModelType
            // 
            ModelType.Text = "Model Type";
            ModelType.Width = 110;
            // 
            // LastPushed
            // 
            LastPushed.Text = "LastPushed";
            LastPushed.Width = 175;
            // 
            // LastModifiedModels
            // 
            LastModifiedModels.Text = "Last Modified";
            LastModifiedModels.Width = 175;
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
            RuntimeFiles_Box.Size = new Size(764, 105);
            RuntimeFiles_Box.TabIndex = 20;
            RuntimeFiles_Box.UseCompatibleStateImageBehavior = false;
            RuntimeFiles_Box.View = View.Details;
            // 
            // RuntimeName
            // 
            RuntimeName.Text = "Name";
            RuntimeName.Width = 279;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Last Source Change";
            columnHeader1.Width = 153;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Last Compliled";
            columnHeader2.Width = 153;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Last Pushed";
            columnHeader3.Width = 153;
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
            Nodes_Box.Columns.AddRange(new ColumnHeader[] { NodeName, IPAddress, columnHeader5, columnHeader6 });
            Nodes_Box.FullRowSelect = true;
            Nodes_Box.GridLines = true;
            Nodes_Box.Location = new Point(12, 531);
            Nodes_Box.Name = "Nodes_Box";
            Nodes_Box.Size = new Size(764, 124);
            Nodes_Box.TabIndex = 21;
            Nodes_Box.UseCompatibleStateImageBehavior = false;
            Nodes_Box.View = View.Details;
            // 
            // NodeName
            // 
            NodeName.Text = "Node Name";
            NodeName.Width = 179;
            // 
            // IPAddress
            // 
            IPAddress.Text = "IP";
            IPAddress.Width = 150;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "Last Uploaded";
            columnHeader5.Width = 175;
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "Last Modified";
            columnHeader6.Width = 175;
            // 
            // Uploader
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(788, 803);
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
            Name = "Uploader";
            Text = "Upload files to camcpp";
            FormClosing += Uploader_FormClosing;
            SystemCTLService_GroupBox.ResumeLayout(false);
            SourceCode_GroupBox.ResumeLayout(false);
            RaspberryPi_GroupBox.ResumeLayout(false);
            SSHSCP_GroupBox.ResumeLayout(false);
            Controls_GroupBox.ResumeLayout(false);
            Controls_GroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private ListView SourceFiles_Box;
        private Button RecheckStatus_Button;
        private Button Reboot_Button;
        private Button Shutdown_Button;
        private ColumnHeader FileName;
        private ColumnHeader LastUploaded;
        private ColumnHeader LastModified;
        private Button Connect;
        private Button StartService_Button;
        private GroupBox SystemCTLService_GroupBox;
        private Button StopService_Button;
        private Button CheckServiceStatus_Button;
        private Button Upload_Button;
        private Button ManualRecompile_Button;
        private GroupBox SourceCode_GroupBox;
        private GroupBox RaspberryPi_GroupBox;
        private GroupBox SSHSCP_GroupBox;
        private GroupBox Controls_GroupBox;
        private Label Status_SSHSCP_Label;
        private Label Status_SystemCTLService_Label;
        private Label Status_RaspberryPi_Label;
        private Label Status_SourceCode_Label;
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
        private ColumnHeader columnHeader5;
        private ColumnHeader columnHeader6;
        private ColumnHeader IsForHub;
        private ColumnHeader IsForNode;
        private ColumnHeader LastPushed;
        private ColumnHeader LastModifiedModels;
        private ColumnHeader IPAddress;
        private ColumnHeader columnHeader1;
        private Label Status_Connction_Label;
        private Button CreateJSONfiles_Button;
        private Button AutoRecompile_Button1;
    }
}
