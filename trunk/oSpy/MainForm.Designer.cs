namespace oSpy
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newCaptureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.closeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageSoftwallRulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timestampDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.eventDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dataSet = new System.Data.DataSet();
            this.eventsTbl = new System.Data.DataTable();
            this.idCol = new System.Data.DataColumn();
            this.timestampCol = new System.Data.DataColumn();
            this.eventCol = new System.Data.DataColumn();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dumpBuilder = new oSpy.SharpDumpLib.DumpBuilder(this.components);
            this.dumpSaver = new oSpy.SharpDumpLib.DumpSaver(this.components);
            this.dumpLoader = new oSpy.SharpDumpLib.DumpLoader(this.components);
            this.menuStrip.SuspendLayout();
            this.dumpContextMenuStrip.SuspendLayout();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventsTbl)).BeginInit();
            this.SuspendLayout();
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "osd";
            this.openFileDialog.Filter = "oSpy dump files|*.osd";
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.DefaultExt = "osd";
            this.saveFileDialog.Filter = "oSpy dump files|*.osd";
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.helpToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(828, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newCaptureToolStripMenuItem,
            this.openMenuItem,
            this.saveMenuItem,
            this.toolStripSeparator1,
            this.closeMenuItem,
            this.toolStripSeparator2,
            this.exitMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpening);
            // 
            // newCaptureToolStripMenuItem
            // 
            this.newCaptureToolStripMenuItem.Name = "newCaptureToolStripMenuItem";
            this.newCaptureToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.newCaptureToolStripMenuItem.Text = "&New capture...";
            this.newCaptureToolStripMenuItem.Click += new System.EventHandler(this.newCaptureToolStripMenuItem_Click);
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openMenuItem.Text = "&Open...";
            this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
            // 
            // saveMenuItem
            // 
            this.saveMenuItem.Name = "saveMenuItem";
            this.saveMenuItem.Size = new System.Drawing.Size(152, 22);
            this.saveMenuItem.Text = "&Save...";
            this.saveMenuItem.Click += new System.EventHandler(this.saveMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(149, 6);
            // 
            // closeMenuItem
            // 
            this.closeMenuItem.Name = "closeMenuItem";
            this.closeMenuItem.Size = new System.Drawing.Size(152, 22);
            this.closeMenuItem.Text = "&Close";
            this.closeMenuItem.Click += new System.EventHandler(this.closeMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(149, 6);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(152, 22);
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.manageSoftwallRulesToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // manageSoftwallRulesToolStripMenuItem
            // 
            this.manageSoftwallRulesToolStripMenuItem.Name = "manageSoftwallRulesToolStripMenuItem";
            this.manageSoftwallRulesToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.manageSoftwallRulesToolStripMenuItem.Text = "&Softwall rules...";
            this.manageSoftwallRulesToolStripMenuItem.Click += new System.EventHandler(this.manageSoftwallRulesToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // dumpContextMenuStrip
            // 
            this.dumpContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem});
            this.dumpContextMenuStrip.Name = "dumpContextMenuStrip";
            this.dumpContextMenuStrip.Size = new System.Drawing.Size(103, 26);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            this.copyToolStripMenuItem.Text = "&Copy";
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 24);
            this.mainSplitContainer.Name = "mainSplitContainer";
            this.mainSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.dataGridView);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.richTextBox);
            this.mainSplitContainer.Size = new System.Drawing.Size(828, 562);
            this.mainSplitContainer.SplitterDistance = 254;
            this.mainSplitContainer.TabIndex = 8;
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AllowUserToResizeRows = false;
            this.dataGridView.AutoGenerateColumns = false;
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.ControlDark;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.idDataGridViewTextBoxColumn,
            this.timestampDataGridViewTextBoxColumn,
            this.eventDataGridViewTextBoxColumn});
            this.dataGridView.DataSource = this.bindingSource;
            this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView.Location = new System.Drawing.Point(0, 0);
            this.dataGridView.MultiSelect = false;
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(828, 254);
            this.dataGridView.TabIndex = 2;
            this.dataGridView.SelectionChanged += new System.EventHandler(this.dataGridView_SelectionChanged);
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.DataPropertyName = "id";
            this.idDataGridViewTextBoxColumn.HeaderText = "id";
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            this.idDataGridViewTextBoxColumn.ReadOnly = true;
            this.idDataGridViewTextBoxColumn.Width = 40;
            // 
            // timestampDataGridViewTextBoxColumn
            // 
            this.timestampDataGridViewTextBoxColumn.DataPropertyName = "timestamp";
            this.timestampDataGridViewTextBoxColumn.HeaderText = "timestamp";
            this.timestampDataGridViewTextBoxColumn.Name = "timestampDataGridViewTextBoxColumn";
            this.timestampDataGridViewTextBoxColumn.ReadOnly = true;
            this.timestampDataGridViewTextBoxColumn.Width = 79;
            // 
            // eventDataGridViewTextBoxColumn
            // 
            this.eventDataGridViewTextBoxColumn.DataPropertyName = "event";
            this.eventDataGridViewTextBoxColumn.HeaderText = "event";
            this.eventDataGridViewTextBoxColumn.Name = "eventDataGridViewTextBoxColumn";
            this.eventDataGridViewTextBoxColumn.ReadOnly = true;
            this.eventDataGridViewTextBoxColumn.Visible = false;
            this.eventDataGridViewTextBoxColumn.Width = 59;
            // 
            // bindingSource
            // 
            this.bindingSource.DataMember = "events";
            this.bindingSource.DataSource = this.dataSet;
            // 
            // dataSet
            // 
            this.dataSet.DataSetName = "events";
            this.dataSet.Tables.AddRange(new System.Data.DataTable[] {
            this.eventsTbl});
            // 
            // eventsTbl
            // 
            this.eventsTbl.Columns.AddRange(new System.Data.DataColumn[] {
            this.idCol,
            this.timestampCol,
            this.eventCol});
            this.eventsTbl.Constraints.AddRange(new System.Data.Constraint[] {
            new System.Data.UniqueConstraint("Constraint1", new string[] {
                        "id"}, false)});
            this.eventsTbl.TableName = "events";
            // 
            // idCol
            // 
            this.idCol.AllowDBNull = false;
            this.idCol.Caption = "Index";
            this.idCol.ColumnName = "id";
            this.idCol.DataType = typeof(uint);
            // 
            // timestampCol
            // 
            this.timestampCol.Caption = "Timestamp";
            this.timestampCol.ColumnName = "timestamp";
            // 
            // eventCol
            // 
            this.eventCol.Caption = "Event";
            this.eventCol.ColumnMapping = System.Data.MappingType.Hidden;
            this.eventCol.ColumnName = "event";
            this.eventCol.DataType = typeof(object);
            // 
            // richTextBox
            // 
            this.richTextBox.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.richTextBox.ContextMenuStrip = this.dumpContextMenuStrip;
            this.richTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox.Font = new System.Drawing.Font("Lucida Console", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox.ForeColor = System.Drawing.Color.Silver;
            this.richTextBox.Location = new System.Drawing.Point(0, 0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.ReadOnly = true;
            this.richTextBox.Size = new System.Drawing.Size(828, 304);
            this.richTextBox.TabIndex = 0;
            this.richTextBox.Text = "";
            this.richTextBox.WordWrap = false;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Event";
            this.dataGridViewTextBoxColumn1.HeaderText = "Event";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Visible = false;
            // 
            // dumpBuilder
            // 
            this.dumpBuilder.BuildProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.curOperation_ProgressChanged);
            this.dumpBuilder.BuildCompleted += new oSpy.SharpDumpLib.BuildCompletedEventHandler(this.dumpBuilder_BuildCompleted);
            // 
            // dumpSaver
            // 
            this.dumpSaver.SaveProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.curOperation_ProgressChanged);
            this.dumpSaver.SaveCompleted += new oSpy.SharpDumpLib.SaveCompletedEventHandler(this.dumpSaver_SaveCompleted);
            // 
            // dumpLoader
            // 
            this.dumpLoader.LoadCompleted += new oSpy.SharpDumpLib.LoadCompletedEventHandler(this.dumpLoader_LoadCompleted);
            this.dumpLoader.LoadProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.curOperation_ProgressChanged);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(828, 585);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "oSpy";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.dumpContextMenuStrip.ResumeLayout(false);
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            this.mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventsTbl)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip dumpContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem manageSoftwallRulesToolStripMenuItem;
        protected System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newCaptureToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private oSpy.SharpDumpLib.DumpBuilder dumpBuilder;
        private System.Data.DataSet dataSet;
        private System.Data.DataTable eventsTbl;
        private System.Data.DataColumn idCol;
        private System.Data.DataColumn timestampCol;
        private System.Data.DataColumn eventCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn timestampDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn eventDataGridViewTextBoxColumn;
        private System.Windows.Forms.BindingSource bindingSource;
        private oSpy.SharpDumpLib.DumpSaver dumpSaver;
        private oSpy.SharpDumpLib.DumpLoader dumpLoader;
    }
}

