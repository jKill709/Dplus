namespace Dplus_Desktop
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Uploader_Button = new Button();
            Viewer_Button = new Button();
            LiveLoggingBox = new RichTextBox();
            Clusters_Box = new ComboBox();
            CurrentCluster_StatusStrip = new Dplus_Desktop.WinForms.Controls.ClusterStatusDisplay.DeviceStatusStrip();
            SuspendLayout();
            // 
            // Uploader_Button
            // 
            Uploader_Button.Location = new Point(79, 12);
            Uploader_Button.Name = "Uploader_Button";
            Uploader_Button.Size = new Size(63, 23);
            Uploader_Button.TabIndex = 0;
            Uploader_Button.Text = "Uploader";
            Uploader_Button.UseVisualStyleBackColor = true;
            Uploader_Button.Click += Uploader_Button_Click;
            // 
            // Viewer_Button
            // 
            Viewer_Button.Location = new Point(12, 12);
            Viewer_Button.Name = "Viewer_Button";
            Viewer_Button.Size = new Size(61, 23);
            Viewer_Button.TabIndex = 1;
            Viewer_Button.Text = "Viewer";
            Viewer_Button.UseVisualStyleBackColor = true;
            Viewer_Button.Click += Viewer_Button_Click;
            // 
            // LiveLoggingBox
            // 
            LiveLoggingBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            LiveLoggingBox.Location = new Point(12, 41);
            LiveLoggingBox.Margin = new Padding(2);
            LiveLoggingBox.Name = "LiveLoggingBox";
            LiveLoggingBox.Size = new Size(953, 527);
            LiveLoggingBox.TabIndex = 2;
            //LiveLoggingBox.Text = "";  // <- Curse this haunted line of code
            // 
            // Clusters_Box
            // 
            Clusters_Box.FormattingEnabled = true;
            Clusters_Box.Location = new Point(150, 12);
            Clusters_Box.Name = "Clusters_Box";
            Clusters_Box.Size = new Size(197, 23);
            Clusters_Box.TabIndex = 19;
            Clusters_Box.SelectedIndexChanged += Clusters_Box_SelectedIndexChanged;
            // 
            // CurrentCluster_StatusStrip
            // 
            CurrentCluster_StatusStrip.Font = new Font("Segoe UI", 8F);
            CurrentCluster_StatusStrip.Location = new Point(353, 4);
            CurrentCluster_StatusStrip.MinimumSize = new Size(150, 25);
            CurrentCluster_StatusStrip.Name = "CurrentCluster_StatusStrip";
            CurrentCluster_StatusStrip.Size = new Size(342, 31);
            CurrentCluster_StatusStrip.TabIndex = 20;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(977, 580);
            Controls.Add(Clusters_Box);
            Controls.Add(CurrentCluster_StatusStrip);
            Controls.Add(LiveLoggingBox);
            Controls.Add(Viewer_Button);
            Controls.Add(Uploader_Button);
            Name = "Main";
            Text = "Main";
            ResumeLayout(false);
        }

        #endregion

        private Button Uploader_Button;
        private Button Viewer_Button;
        private RichTextBox LiveLoggingBox;
        private ComboBox Clusters_Box;
        private WinForms.Controls.ClusterStatusDisplay.DeviceStatusStrip CurrentCluster_StatusStrip;
    }
}