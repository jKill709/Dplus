namespace Dplus_Desktop
{
    partial class Viewer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Viewer));
            SavedFrames_Box = new ComboBox();
            Image2_View = new ImageControls();
            Image1_View = new ImageControls();
            ImageType_Box = new ComboBox();
            XYView = new OrthographicViewer();
            XZView = new OrthographicViewer();
            YZView = new OrthographicViewer();
            ShowIDs_Box = new CheckBox();
            useDetectorImages_Box = new CheckBox();
            CurrentFrame_View = new TreeView();
            Objectness_Bar = new TrackBar();
            KP_Bar = new TrackBar();
            ObjectnessValue_Label = new Label();
            KpValue_Label = new Label();
            SavedFramesBoxIndex_UpDown = new NumericUpDown();
            SavedFrameIndex_Label = new Label();
            PerspectiveView = new PerspectiveViewer();
            ShowDebug_Box = new CheckBox();
            SavedFrames_rButton = new RadioButton();
            SavedFrames_Group = new GroupBox();
            SavedFramesRun_Box = new ComboBox();
            SavedFramesRunBoxIndex_UpDown = new NumericUpDown();
            SavedFrameRunIndex_Label = new Label();
            LiveView_Group = new GroupBox();
            LiveView_ToolStrip = new ToolStrip();
            LiveViewStatus_Label = new ToolStripLabel();
            LiveViewInfo_Label = new ToolStripLabel();
            LiveViewTimestamp_Label = new ToolStripLabel();
            LiveViewFPS_Label = new ToolStripLabel();
            LiveCatchup_Button = new Button();
            LiveNext_Button = new Button();
            LivePause_Button = new Button();
            LivePrevious_Button = new Button();
            LiveRewind_Button = new Button();
            LiveFrames_Bar = new TrackBar();
            LiveView_rButton = new RadioButton();
            panel1 = new Panel();
            DataSource_PanalLabel = new Label();
            Autofit_Button = new Button();
            ((System.ComponentModel.ISupportInitialize)Objectness_Bar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)KP_Bar).BeginInit();
            ((System.ComponentModel.ISupportInitialize)SavedFramesBoxIndex_UpDown).BeginInit();
            SavedFrames_Group.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)SavedFramesRunBoxIndex_UpDown).BeginInit();
            LiveView_Group.SuspendLayout();
            LiveView_ToolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)LiveFrames_Bar).BeginInit();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // SavedFrames_Box
            // 
            SavedFrames_Box.FormattingEnabled = true;
            SavedFrames_Box.Location = new Point(8, 52);
            SavedFrames_Box.Name = "SavedFrames_Box";
            SavedFrames_Box.Size = new Size(121, 23);
            SavedFrames_Box.TabIndex = 0;
            SavedFrames_Box.SelectedIndexChanged += SavedFrames_Box_SelectedIndexChanged;
            // 
            // Image2_View
            // 
            Image2_View.BackColor = SystemColors.ControlDarkDark;
            Image2_View.DisplayedImage = null;
            Image2_View.Location = new Point(1374, 428);
            Image2_View.Name = "Image2_View";
            Image2_View.Size = new Size(480, 360);
            Image2_View.TabIndex = 6;
            Image2_View.Text = "Image2";
            // 
            // Image1_View
            // 
            Image1_View.BackColor = SystemColors.ControlDarkDark;
            Image1_View.DisplayedImage = null;
            Image1_View.Location = new Point(1374, 62);
            Image1_View.Name = "Image1_View";
            Image1_View.Size = new Size(480, 360);
            Image1_View.TabIndex = 5;
            Image1_View.Text = "Image1";
            // 
            // ImageType_Box
            // 
            ImageType_Box.FormattingEnabled = true;
            ImageType_Box.Location = new Point(924, 30);
            ImageType_Box.Name = "ImageType_Box";
            ImageType_Box.Size = new Size(121, 23);
            ImageType_Box.TabIndex = 7;
            ImageType_Box.SelectedIndexChanged += UpdateViewers;
            // 
            // XYView
            // 
            XYView.GridSpacing = 10F;
            XYView.Location = new Point(462, 121);
            XYView.Name = "XYView";
            XYView.ShowAxes = true;
            XYView.ShowDebug = false;
            XYView.ShowGrid = true;
            XYView.Size = new Size(450, 337);
            XYView.TabIndex = 8;
            XYView.Text = "XYView";
            XYView.Click += XYViewer_Click;
            // 
            // XZView
            // 
            XZView.GridSpacing = 10F;
            XZView.Location = new Point(918, 121);
            XZView.Name = "XZView";
            XZView.ShowAxes = true;
            XZView.ShowDebug = false;
            XZView.ShowGrid = true;
            XZView.Size = new Size(450, 337);
            XZView.TabIndex = 9;
            XZView.Text = "XZView";
            // 
            // YZView
            // 
            YZView.GridSpacing = 10F;
            YZView.Location = new Point(918, 464);
            YZView.Name = "YZView";
            YZView.ShowAxes = true;
            YZView.ShowDebug = false;
            YZView.ShowGrid = true;
            YZView.Size = new Size(450, 337);
            YZView.TabIndex = 10;
            YZView.Text = "YZView";
            // 
            // ShowIDs_Box
            // 
            ShowIDs_Box.AutoSize = true;
            ShowIDs_Box.Checked = true;
            ShowIDs_Box.CheckState = CheckState.Checked;
            ShowIDs_Box.Location = new Point(1053, 33);
            ShowIDs_Box.Name = "ShowIDs_Box";
            ShowIDs_Box.Size = new Size(74, 19);
            ShowIDs_Box.TabIndex = 11;
            ShowIDs_Box.Text = "Show IDs";
            ShowIDs_Box.UseVisualStyleBackColor = true;
            ShowIDs_Box.CheckedChanged += UpdateViewers;
            // 
            // useDetectorImages_Box
            // 
            useDetectorImages_Box.AutoSize = true;
            useDetectorImages_Box.Checked = true;
            useDetectorImages_Box.CheckState = CheckState.Checked;
            useDetectorImages_Box.Location = new Point(1053, 58);
            useDetectorImages_Box.Name = "useDetectorImages_Box";
            useDetectorImages_Box.Size = new Size(112, 19);
            useDetectorImages_Box.TabIndex = 12;
            useDetectorImages_Box.Text = "Detector Images";
            useDetectorImages_Box.UseVisualStyleBackColor = true;
            useDetectorImages_Box.CheckedChanged += UpdateViewers;
            // 
            // CurrentFrame_View
            // 
            CurrentFrame_View.Location = new Point(2, 206);
            CurrentFrame_View.Name = "CurrentFrame_View";
            CurrentFrame_View.Size = new Size(453, 595);
            CurrentFrame_View.TabIndex = 13;
            // 
            // Objectness_Bar
            // 
            Objectness_Bar.LargeChange = 10;
            Objectness_Bar.Location = new Point(462, 27);
            Objectness_Bar.Maximum = 100;
            Objectness_Bar.Name = "Objectness_Bar";
            Objectness_Bar.Size = new Size(454, 45);
            Objectness_Bar.SmallChange = 2;
            Objectness_Bar.TabIndex = 14;
            Objectness_Bar.ValueChanged += Objectness_Bar_ValueChanged;
            // 
            // KP_Bar
            // 
            KP_Bar.LargeChange = 10;
            KP_Bar.Location = new Point(462, 78);
            KP_Bar.Maximum = 100;
            KP_Bar.Name = "KP_Bar";
            KP_Bar.Size = new Size(454, 45);
            KP_Bar.SmallChange = 2;
            KP_Bar.TabIndex = 15;
            KP_Bar.ValueChanged += KP_Bar_ValueChanged;
            // 
            // ObjectnessValue_Label
            // 
            ObjectnessValue_Label.AutoSize = true;
            ObjectnessValue_Label.Location = new Point(670, 52);
            ObjectnessValue_Label.Name = "ObjectnessValue_Label";
            ObjectnessValue_Label.Size = new Size(13, 15);
            ObjectnessValue_Label.TabIndex = 16;
            ObjectnessValue_Label.Text = "0";
            ObjectnessValue_Label.TextAlign = ContentAlignment.TopCenter;
            // 
            // KpValue_Label
            // 
            KpValue_Label.AutoSize = true;
            KpValue_Label.Location = new Point(670, 103);
            KpValue_Label.Name = "KpValue_Label";
            KpValue_Label.Size = new Size(13, 15);
            KpValue_Label.TabIndex = 17;
            KpValue_Label.Text = "0";
            KpValue_Label.TextAlign = ContentAlignment.TopCenter;
            // 
            // SavedFramesBoxIndex_UpDown
            // 
            SavedFramesBoxIndex_UpDown.Location = new Point(128, 52);
            SavedFramesBoxIndex_UpDown.Name = "SavedFramesBoxIndex_UpDown";
            SavedFramesBoxIndex_UpDown.Size = new Size(18, 23);
            SavedFramesBoxIndex_UpDown.TabIndex = 18;
            SavedFramesBoxIndex_UpDown.ValueChanged += SavedFramesBoxIndex_UpDown_ValueChanged;
            // 
            // SavedFrameIndex_Label
            // 
            SavedFrameIndex_Label.AutoSize = true;
            SavedFrameIndex_Label.Location = new Point(152, 54);
            SavedFrameIndex_Label.Name = "SavedFrameIndex_Label";
            SavedFrameIndex_Label.Size = new Size(24, 15);
            SavedFrameIndex_Label.TabIndex = 19;
            SavedFrameIndex_Label.Text = "0/0";
            // 
            // PerspectiveView
            // 
            PerspectiveView.BackColor = Color.DarkGray;
            PerspectiveView.Location = new Point(462, 464);
            PerspectiveView.Name = "PerspectiveView";
            PerspectiveView.ShowAxes = true;
            PerspectiveView.ShowDebug = false;
            PerspectiveView.Size = new Size(450, 337);
            PerspectiveView.TabIndex = 20;
            PerspectiveView.Text = "perspectiveViewer1";
            // 
            // ShowDebug_Box
            // 
            ShowDebug_Box.AutoSize = true;
            ShowDebug_Box.Location = new Point(1178, 32);
            ShowDebug_Box.Name = "ShowDebug_Box";
            ShowDebug_Box.Size = new Size(93, 19);
            ShowDebug_Box.TabIndex = 21;
            ShowDebug_Box.Text = "Show Debug";
            ShowDebug_Box.UseVisualStyleBackColor = true;
            ShowDebug_Box.CheckedChanged += ShowDebug_Box_CheckedChanged;
            // 
            // SavedFrames_rButton
            // 
            SavedFrames_rButton.AutoSize = true;
            SavedFrames_rButton.Location = new Point(3, 3);
            SavedFrames_rButton.Name = "SavedFrames_rButton";
            SavedFrames_rButton.Size = new Size(97, 19);
            SavedFrames_rButton.TabIndex = 22;
            SavedFrames_rButton.Text = "Saved Frames";
            SavedFrames_rButton.UseVisualStyleBackColor = true;
            SavedFrames_rButton.CheckedChanged += SavedFrames_rButton_CheckedChanged;
            // 
            // SavedFrames_Group
            // 
            SavedFrames_Group.Controls.Add(SavedFramesRun_Box);
            SavedFrames_Group.Controls.Add(SavedFramesRunBoxIndex_UpDown);
            SavedFrames_Group.Controls.Add(SavedFrameRunIndex_Label);
            SavedFrames_Group.Controls.Add(SavedFrames_Box);
            SavedFrames_Group.Controls.Add(SavedFramesBoxIndex_UpDown);
            SavedFrames_Group.Controls.Add(SavedFrameIndex_Label);
            SavedFrames_Group.Location = new Point(3, 21);
            SavedFrames_Group.Name = "SavedFrames_Group";
            SavedFrames_Group.Size = new Size(219, 152);
            SavedFrames_Group.TabIndex = 23;
            SavedFrames_Group.TabStop = false;
            // 
            // SavedFramesRun_Box
            // 
            SavedFramesRun_Box.FormattingEnabled = true;
            SavedFramesRun_Box.Location = new Point(8, 22);
            SavedFramesRun_Box.Name = "SavedFramesRun_Box";
            SavedFramesRun_Box.Size = new Size(121, 23);
            SavedFramesRun_Box.TabIndex = 20;
            // 
            // SavedFramesRunBoxIndex_UpDown
            // 
            SavedFramesRunBoxIndex_UpDown.Location = new Point(128, 22);
            SavedFramesRunBoxIndex_UpDown.Name = "SavedFramesRunBoxIndex_UpDown";
            SavedFramesRunBoxIndex_UpDown.Size = new Size(18, 23);
            SavedFramesRunBoxIndex_UpDown.TabIndex = 21;
            // 
            // SavedFrameRunIndex_Label
            // 
            SavedFrameRunIndex_Label.AutoSize = true;
            SavedFrameRunIndex_Label.Location = new Point(152, 24);
            SavedFrameRunIndex_Label.Name = "SavedFrameRunIndex_Label";
            SavedFrameRunIndex_Label.Size = new Size(24, 15);
            SavedFrameRunIndex_Label.TabIndex = 22;
            SavedFrameRunIndex_Label.Text = "0/0";
            // 
            // LiveView_Group
            // 
            LiveView_Group.Controls.Add(LiveView_ToolStrip);
            LiveView_Group.Controls.Add(LiveCatchup_Button);
            LiveView_Group.Controls.Add(LiveNext_Button);
            LiveView_Group.Controls.Add(LivePause_Button);
            LiveView_Group.Controls.Add(LivePrevious_Button);
            LiveView_Group.Controls.Add(LiveRewind_Button);
            LiveView_Group.Controls.Add(LiveFrames_Bar);
            LiveView_Group.Location = new Point(227, 21);
            LiveView_Group.Name = "LiveView_Group";
            LiveView_Group.Size = new Size(219, 152);
            LiveView_Group.TabIndex = 24;
            LiveView_Group.TabStop = false;
            // 
            // LiveView_ToolStrip
            // 
            LiveView_ToolStrip.Dock = DockStyle.Bottom;
            LiveView_ToolStrip.Items.AddRange(new ToolStripItem[] { LiveViewStatus_Label, LiveViewInfo_Label, LiveViewTimestamp_Label, LiveViewFPS_Label });
            LiveView_ToolStrip.Location = new Point(3, 124);
            LiveView_ToolStrip.Name = "LiveView_ToolStrip";
            LiveView_ToolStrip.Size = new Size(213, 25);
            LiveView_ToolStrip.TabIndex = 7;
            // 
            // LiveViewStatus_Label
            // 
            LiveViewStatus_Label.BackgroundImageLayout = ImageLayout.None;
            LiveViewStatus_Label.DisplayStyle = ToolStripItemDisplayStyle.Text;
            LiveViewStatus_Label.Name = "LiveViewStatus_Label";
            LiveViewStatus_Label.Size = new Size(43, 22);
            LiveViewStatus_Label.Text = "Discon";
            // 
            // LiveViewInfo_Label
            // 
            LiveViewInfo_Label.DisplayStyle = ToolStripItemDisplayStyle.Text;
            LiveViewInfo_Label.Image = (Image)resources.GetObject("LiveViewInfo_Label.Image");
            LiveViewInfo_Label.ImageTransparentColor = Color.Magenta;
            LiveViewInfo_Label.Name = "LiveViewInfo_Label";
            LiveViewInfo_Label.Size = new Size(0, 22);
            // 
            // LiveViewTimestamp_Label
            // 
            LiveViewTimestamp_Label.Name = "LiveViewTimestamp_Label";
            LiveViewTimestamp_Label.Size = new Size(49, 22);
            LiveViewTimestamp_Label.Text = "00:00:00";
            // 
            // LiveViewFPS_Label
            // 
            LiveViewFPS_Label.Alignment = ToolStripItemAlignment.Right;
            LiveViewFPS_Label.DisplayStyle = ToolStripItemDisplayStyle.Text;
            LiveViewFPS_Label.Image = (Image)resources.GetObject("LiveViewFPS_Label.Image");
            LiveViewFPS_Label.ImageTransparentColor = Color.Magenta;
            LiveViewFPS_Label.Name = "LiveViewFPS_Label";
            LiveViewFPS_Label.Size = new Size(35, 22);
            LiveViewFPS_Label.Text = "0 FPS";
            // 
            // LiveCatchup_Button
            // 
            LiveCatchup_Button.BackgroundImage = Properties.Resources.PlayButtonGreen;
            LiveCatchup_Button.BackgroundImageLayout = ImageLayout.Stretch;
            LiveCatchup_Button.Location = new Point(176, 72);
            LiveCatchup_Button.Name = "LiveCatchup_Button";
            LiveCatchup_Button.Size = new Size(37, 37);
            LiveCatchup_Button.TabIndex = 6;
            LiveCatchup_Button.UseVisualStyleBackColor = true;
            LiveCatchup_Button.Click += LiveCatchup_Button_Click;
            // 
            // LiveNext_Button
            // 
            LiveNext_Button.BackgroundImage = Properties.Resources.NextButtonGreen;
            LiveNext_Button.BackgroundImageLayout = ImageLayout.Stretch;
            LiveNext_Button.Location = new Point(133, 72);
            LiveNext_Button.Name = "LiveNext_Button";
            LiveNext_Button.Size = new Size(37, 37);
            LiveNext_Button.TabIndex = 5;
            LiveNext_Button.UseVisualStyleBackColor = true;
            LiveNext_Button.Click += LiveNext_Button_Click;
            // 
            // LivePause_Button
            // 
            LivePause_Button.BackgroundImage = Properties.Resources.PauseButtonRed;
            LivePause_Button.BackgroundImageLayout = ImageLayout.Stretch;
            LivePause_Button.Location = new Point(91, 73);
            LivePause_Button.Name = "LivePause_Button";
            LivePause_Button.Size = new Size(37, 37);
            LivePause_Button.TabIndex = 4;
            LivePause_Button.UseVisualStyleBackColor = true;
            LivePause_Button.Click += LivePause_Button_Click;
            // 
            // LivePrevious_Button
            // 
            LivePrevious_Button.BackgroundImage = Properties.Resources.PreviousButtonGreen;
            LivePrevious_Button.BackgroundImageLayout = ImageLayout.Stretch;
            LivePrevious_Button.Location = new Point(48, 73);
            LivePrevious_Button.Name = "LivePrevious_Button";
            LivePrevious_Button.Size = new Size(37, 37);
            LivePrevious_Button.TabIndex = 3;
            LivePrevious_Button.UseVisualStyleBackColor = true;
            LivePrevious_Button.Click += LivePrevious_Button_Click;
            // 
            // LiveRewind_Button
            // 
            LiveRewind_Button.BackgroundImage = Properties.Resources.RewindButtonGreen;
            LiveRewind_Button.BackgroundImageLayout = ImageLayout.Stretch;
            LiveRewind_Button.Location = new Point(6, 73);
            LiveRewind_Button.Name = "LiveRewind_Button";
            LiveRewind_Button.Size = new Size(37, 37);
            LiveRewind_Button.TabIndex = 2;
            LiveRewind_Button.UseVisualStyleBackColor = true;
            LiveRewind_Button.Click += LiveRewind_Button_Click;
            // 
            // LiveFrames_Bar
            // 
            LiveFrames_Bar.LargeChange = 1;
            LiveFrames_Bar.Location = new Point(6, 22);
            LiveFrames_Bar.Maximum = 0;
            LiveFrames_Bar.Name = "LiveFrames_Bar";
            LiveFrames_Bar.Size = new Size(207, 45);
            LiveFrames_Bar.TabIndex = 0;
            LiveFrames_Bar.ValueChanged += LiveFrames_Bar_ValueChanged;
            // 
            // LiveView_rButton
            // 
            LiveView_rButton.AutoSize = true;
            LiveView_rButton.Location = new Point(227, 3);
            LiveView_rButton.Name = "LiveView_rButton";
            LiveView_rButton.Size = new Size(71, 19);
            LiveView_rButton.TabIndex = 22;
            LiveView_rButton.Text = "LiveView";
            LiveView_rButton.UseVisualStyleBackColor = true;
            LiveView_rButton.CheckedChanged += LiveView_rButton_CheckedChanged;
            // 
            // panel1
            // 
            panel1.Controls.Add(LiveView_rButton);
            panel1.Controls.Add(SavedFrames_rButton);
            panel1.Controls.Add(LiveView_Group);
            panel1.Controls.Add(SavedFrames_Group);
            panel1.Location = new Point(7, 27);
            panel1.Name = "panel1";
            panel1.Size = new Size(449, 173);
            panel1.TabIndex = 25;
            // 
            // DataSource_PanalLabel
            // 
            DataSource_PanalLabel.AutoSize = true;
            DataSource_PanalLabel.Location = new Point(10, 12);
            DataSource_PanalLabel.Name = "DataSource_PanalLabel";
            DataSource_PanalLabel.Size = new Size(70, 15);
            DataSource_PanalLabel.TabIndex = 26;
            DataSource_PanalLabel.Text = "Data Source";
            // 
            // Autofit_Button
            // 
            Autofit_Button.Location = new Point(1181, 55);
            Autofit_Button.Name = "Autofit_Button";
            Autofit_Button.Size = new Size(75, 23);
            Autofit_Button.TabIndex = 27;
            Autofit_Button.Text = "Autofit";
            Autofit_Button.UseVisualStyleBackColor = true;
            Autofit_Button.Click += Autofit_Button_Click;
            // 
            // Viewer
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1876, 853);
            Controls.Add(Autofit_Button);
            Controls.Add(DataSource_PanalLabel);
            Controls.Add(ShowDebug_Box);
            Controls.Add(PerspectiveView);
            Controls.Add(KpValue_Label);
            Controls.Add(KP_Bar);
            Controls.Add(CurrentFrame_View);
            Controls.Add(useDetectorImages_Box);
            Controls.Add(ShowIDs_Box);
            Controls.Add(YZView);
            Controls.Add(XZView);
            Controls.Add(XYView);
            Controls.Add(ImageType_Box);
            Controls.Add(Image2_View);
            Controls.Add(Image1_View);
            Controls.Add(ObjectnessValue_Label);
            Controls.Add(Objectness_Bar);
            Controls.Add(panel1);
            Name = "Viewer";
            Text = "Viewer";
            FormClosing += Viewer_FormClosing;
            Load += Viewer_Load;
            ((System.ComponentModel.ISupportInitialize)Objectness_Bar).EndInit();
            ((System.ComponentModel.ISupportInitialize)KP_Bar).EndInit();
            ((System.ComponentModel.ISupportInitialize)SavedFramesBoxIndex_UpDown).EndInit();
            SavedFrames_Group.ResumeLayout(false);
            SavedFrames_Group.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)SavedFramesRunBoxIndex_UpDown).EndInit();
            LiveView_Group.ResumeLayout(false);
            LiveView_Group.PerformLayout();
            LiveView_ToolStrip.ResumeLayout(false);
            LiveView_ToolStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)LiveFrames_Bar).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ComboBox SavedFrames_Box;
        private ImageControls Image2_View;
        private ImageControls Image1_View;
        private ComboBox ImageType_Box;
        private OrthographicViewer XYView;
        private OrthographicViewer XZView;
        private OrthographicViewer YZView;
        private CheckBox ShowIDs_Box;
        private CheckBox useDetectorImages_Box;
        private TreeView CurrentFrame_View;
        private TrackBar Objectness_Bar;
        private TrackBar KP_Bar;
        private Label ObjectnessValue_Label;
        private Label KpValue_Label;
        private NumericUpDown SavedFramesBoxIndex_UpDown;
        private Label SavedFrameIndex_Label;
        private PerspectiveViewer PerspectiveView;
        private CheckBox ShowDebug_Box;
        private RadioButton SavedFrames_rButton;
        private GroupBox SavedFrames_Group;
        private GroupBox LiveView_Group;
        private RadioButton LiveView_rButton;
        private Panel panel1;
        private Label DataSource_PanalLabel;
        private TrackBar LiveFrames_Bar;
        private Button LiveRewind_Button;
        private Button LiveCatchup_Button;
        private Button LiveNext_Button;
        private Button LivePause_Button;
        private Button LivePrevious_Button;
        private ToolStripSeparator toolStripSeparator1;
        private ComboBox SavedFramesRun_Box;
        private NumericUpDown SavedFramesRunBoxIndex_UpDown;
        private Label SavedFrameRunIndex_Label;
        private ToolStrip LiveView_ToolStrip;
        private ToolStripLabel LiveViewStatus_Label;
        private ToolStripLabel LiveViewInfo_Label;
        private ToolStripLabel LiveViewTimestamp_Label;
        private ToolStripLabel LiveViewFPS_Label;
        private Button Autofit_Button;
    }
}