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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.dataSet = new System.Data.DataSet();
            this.messageTbl = new System.Data.DataTable();
            this.indexCol = new System.Data.DataColumn();
            this.timestampCol = new System.Data.DataColumn();
            this.processNameCol = new System.Data.DataColumn();
            this.processIdCol = new System.Data.DataColumn();
            this.threadIdCol = new System.Data.DataColumn();
            this.functionNameCol = new System.Data.DataColumn();
            this.returnAddressCol = new System.Data.DataColumn();
            this.callerModuleNameCol = new System.Data.DataColumn();
            this.resourceIdCol = new System.Data.DataColumn();
            this.msgTypeCol = new System.Data.DataColumn();
            this.contextCol = new System.Data.DataColumn();
            this.messageCol = new System.Data.DataColumn();
            this.directionCol = new System.Data.DataColumn();
            this.localAddressCol = new System.Data.DataColumn();
            this.localPortCol = new System.Data.DataColumn();
            this.peerAddressCol = new System.Data.DataColumn();
            this.peerPortCol = new System.Data.DataColumn();
            this.dataCol = new System.Data.DataColumn();
            this.ASDeviceCol = new System.Data.DataColumn();
            this.ASStatusCol = new System.Data.DataColumn();
            this.ASSubStatusCol = new System.Data.DataColumn();
            this.ASWizStatusCol = new System.Data.DataColumn();
            this.senderCol = new System.Data.DataColumn();
            this.descriptionCol = new System.Data.DataColumn();
            this.bgColorCol = new System.Data.DataColumn();
            this.commentCol = new System.Data.DataColumn();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.clearMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectAlltransactionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.captureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.injectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.captureStartMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageSoftwallRulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.parserConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.analyzeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showMSNP2PConversationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.goToReturnaddressInIDAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.createSwRuleFromEntryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dumpContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveRawDumpToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripSeparator();
            this.showAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aSCIIToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.hexToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.propertyGridContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.expandAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.collapseAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.autoHighlightMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.indexDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.typeDataGridViewImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.timestampDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.processNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.processIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.threadIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.functionNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.returnAddressDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.callerModuleNameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.resourceIdDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.msgTypeDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.msgContextDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.messageDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.directionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.localAddressDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.localPortDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.peerAddressDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.peerPortDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataDataGridViewImageColumn = new System.Windows.Forms.DataGridViewImageColumn();
            this.aSDeviceDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aSStatusDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aSSubStatusDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.aSWizStatusDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.senderDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.descriptionDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Comment = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bgColorDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.filterComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.findTypeComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.findComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusBarLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.dumpSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.messageTbl)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.dataGridContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).BeginInit();
            this.dumpContextMenuStrip.SuspendLayout();
            this.propertyGridContextMenuStrip.SuspendLayout();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataSet
            // 
            this.dataSet.CaseSensitive = true;
            this.dataSet.DataSetName = "oSpyDataSet";
            this.dataSet.Tables.AddRange(new System.Data.DataTable[] {
            this.messageTbl});
            // 
            // messageTbl
            // 
            this.messageTbl.Columns.AddRange(new System.Data.DataColumn[] {
            this.indexCol,
            this.timestampCol,
            this.processNameCol,
            this.processIdCol,
            this.threadIdCol,
            this.functionNameCol,
            this.returnAddressCol,
            this.callerModuleNameCol,
            this.resourceIdCol,
            this.msgTypeCol,
            this.contextCol,
            this.messageCol,
            this.directionCol,
            this.localAddressCol,
            this.localPortCol,
            this.peerAddressCol,
            this.peerPortCol,
            this.dataCol,
            this.ASDeviceCol,
            this.ASStatusCol,
            this.ASSubStatusCol,
            this.ASWizStatusCol,
            this.senderCol,
            this.descriptionCol,
            this.bgColorCol,
            this.commentCol});
            this.messageTbl.Constraints.AddRange(new System.Data.Constraint[] {
            new System.Data.UniqueConstraint("Constraint1", new string[] {
                        "Index"}, true)});
            this.messageTbl.PrimaryKey = new System.Data.DataColumn[] {
        this.indexCol};
            this.messageTbl.TableName = "Messages";
            this.messageTbl.RowChanged += new System.Data.DataRowChangeEventHandler(this.messageTbl_RowChanged);
            // 
            // indexCol
            // 
            this.indexCol.AllowDBNull = false;
            this.indexCol.AutoIncrement = true;
            this.indexCol.Caption = "Index";
            this.indexCol.ColumnName = "Index";
            this.indexCol.DataType = typeof(int);
            // 
            // timestampCol
            // 
            this.timestampCol.AllowDBNull = false;
            this.timestampCol.Caption = "Timestamp";
            this.timestampCol.ColumnName = "Timestamp";
            this.timestampCol.DataType = typeof(System.DateTime);
            this.timestampCol.DateTimeMode = System.Data.DataSetDateTime.Local;
            // 
            // processNameCol
            // 
            this.processNameCol.AllowDBNull = false;
            this.processNameCol.Caption = "Process name";
            this.processNameCol.ColumnName = "ProcessName";
            // 
            // processIdCol
            // 
            this.processIdCol.AllowDBNull = false;
            this.processIdCol.Caption = "Process Id";
            this.processIdCol.ColumnName = "ProcessId";
            this.processIdCol.DataType = typeof(uint);
            // 
            // threadIdCol
            // 
            this.threadIdCol.AllowDBNull = false;
            this.threadIdCol.Caption = "Thread Id";
            this.threadIdCol.ColumnName = "ThreadId";
            this.threadIdCol.DataType = typeof(uint);
            // 
            // functionNameCol
            // 
            this.functionNameCol.AllowDBNull = false;
            this.functionNameCol.Caption = "Function name";
            this.functionNameCol.ColumnName = "FunctionName";
            // 
            // returnAddressCol
            // 
            this.returnAddressCol.AllowDBNull = false;
            this.returnAddressCol.ColumnName = "ReturnAddress";
            this.returnAddressCol.DataType = typeof(uint);
            // 
            // callerModuleNameCol
            // 
            this.callerModuleNameCol.Caption = "CallerModuleName";
            this.callerModuleNameCol.ColumnName = "CallerModuleName";
            // 
            // resourceIdCol
            // 
            this.resourceIdCol.ColumnName = "ResourceId";
            this.resourceIdCol.DataType = typeof(uint);
            // 
            // msgTypeCol
            // 
            this.msgTypeCol.AllowDBNull = false;
            this.msgTypeCol.Caption = "Message type";
            this.msgTypeCol.ColumnName = "MsgType";
            this.msgTypeCol.DataType = typeof(uint);
            // 
            // contextCol
            // 
            this.contextCol.Caption = "Message context";
            this.contextCol.ColumnName = "MsgContext";
            this.contextCol.DataType = typeof(uint);
            // 
            // messageCol
            // 
            this.messageCol.Caption = "Message";
            this.messageCol.ColumnName = "Message";
            // 
            // directionCol
            // 
            this.directionCol.Caption = "Packet direction";
            this.directionCol.ColumnName = "Direction";
            this.directionCol.DataType = typeof(uint);
            // 
            // localAddressCol
            // 
            this.localAddressCol.Caption = "Local address";
            this.localAddressCol.ColumnName = "LocalAddress";
            this.localAddressCol.MaxLength = 15;
            // 
            // localPortCol
            // 
            this.localPortCol.Caption = "Local port";
            this.localPortCol.ColumnName = "LocalPort";
            this.localPortCol.DataType = typeof(ushort);
            // 
            // peerAddressCol
            // 
            this.peerAddressCol.Caption = "Peer address";
            this.peerAddressCol.ColumnName = "PeerAddress";
            this.peerAddressCol.MaxLength = 15;
            // 
            // peerPortCol
            // 
            this.peerPortCol.Caption = "Peer port";
            this.peerPortCol.ColumnName = "PeerPort";
            this.peerPortCol.DataType = typeof(ushort);
            // 
            // dataCol
            // 
            this.dataCol.Caption = "Data";
            this.dataCol.ColumnName = "Data";
            this.dataCol.DataType = typeof(byte[]);
            // 
            // ASDeviceCol
            // 
            this.ASDeviceCol.ColumnName = "AS_Device";
            // 
            // ASStatusCol
            // 
            this.ASStatusCol.ColumnName = "AS_Status";
            // 
            // ASSubStatusCol
            // 
            this.ASSubStatusCol.ColumnName = "AS_SubStatus";
            // 
            // ASWizStatusCol
            // 
            this.ASWizStatusCol.ColumnName = "AS_WizStatus";
            // 
            // senderCol
            // 
            this.senderCol.ColumnMapping = System.Data.MappingType.Hidden;
            this.senderCol.ColumnName = "Sender";
            // 
            // descriptionCol
            // 
            this.descriptionCol.ColumnMapping = System.Data.MappingType.Hidden;
            this.descriptionCol.ColumnName = "Description";
            // 
            // bgColorCol
            // 
            this.bgColorCol.ColumnMapping = System.Data.MappingType.Hidden;
            this.bgColorCol.ColumnName = "BgColor";
            this.bgColorCol.DataType = typeof(byte[]);
            // 
            // commentCol
            // 
            this.commentCol.ColumnMapping = System.Data.MappingType.Hidden;
            this.commentCol.ColumnName = "Comment";
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "osd";
            this.openFileDialog.FileName = "openFileDialog1";
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
            this.editToolStripMenuItem,
            this.captureToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.analyzeToolStripMenuItem,
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
            this.openMenuItem,
            this.saveMenuItem,
            this.toolStripSeparator1,
            this.clearMenuItem,
            this.toolStripSeparator2,
            this.exitMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Size = new System.Drawing.Size(123, 22);
            this.openMenuItem.Text = "&Open...";
            this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
            // 
            // saveMenuItem
            // 
            this.saveMenuItem.Name = "saveMenuItem";
            this.saveMenuItem.Size = new System.Drawing.Size(123, 22);
            this.saveMenuItem.Text = "&Save...";
            this.saveMenuItem.Click += new System.EventHandler(this.saveMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(120, 6);
            // 
            // clearMenuItem
            // 
            this.clearMenuItem.Name = "clearMenuItem";
            this.clearMenuItem.Size = new System.Drawing.Size(123, 22);
            this.clearMenuItem.Text = "&Clear";
            this.clearMenuItem.Click += new System.EventHandler(this.clearMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(120, 6);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(123, 22);
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectAlltransactionsToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // selectAlltransactionsToolStripMenuItem
            // 
            this.selectAlltransactionsToolStripMenuItem.Name = "selectAlltransactionsToolStripMenuItem";
            this.selectAlltransactionsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.selectAlltransactionsToolStripMenuItem.Text = "Select all &transactions";
            this.selectAlltransactionsToolStripMenuItem.Click += new System.EventHandler(this.selectAlltransactionsToolStripMenuItem_Click);
            // 
            // captureToolStripMenuItem
            // 
            this.captureToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.injectToolStripMenuItem,
            this.captureStartMenuItem});
            this.captureToolStripMenuItem.Name = "captureToolStripMenuItem";
            this.captureToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.captureToolStripMenuItem.Text = "&Capture";
            // 
            // injectToolStripMenuItem
            // 
            this.injectToolStripMenuItem.Name = "injectToolStripMenuItem";
            this.injectToolStripMenuItem.Size = new System.Drawing.Size(156, 22);
            this.injectToolStripMenuItem.Text = "&Inject agent...";
            this.injectToolStripMenuItem.Click += new System.EventHandler(this.injectToolStripMenuItem_Click);
            // 
            // captureStartMenuItem
            // 
            this.captureStartMenuItem.Name = "captureStartMenuItem";
            this.captureStartMenuItem.Size = new System.Drawing.Size(156, 22);
            this.captureStartMenuItem.Text = "&Start";
            this.captureStartMenuItem.Click += new System.EventHandler(this.captureStartMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.manageSoftwallRulesToolStripMenuItem,
            this.parserConfigToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(56, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // manageSoftwallRulesToolStripMenuItem
            // 
            this.manageSoftwallRulesToolStripMenuItem.Name = "manageSoftwallRulesToolStripMenuItem";
            this.manageSoftwallRulesToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.manageSoftwallRulesToolStripMenuItem.Text = "&Softwall rules...";
            this.manageSoftwallRulesToolStripMenuItem.Click += new System.EventHandler(this.manageSoftwallRulesToolStripMenuItem_Click);
            // 
            // parserConfigToolStripMenuItem
            // 
            this.parserConfigToolStripMenuItem.Name = "parserConfigToolStripMenuItem";
            this.parserConfigToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.parserConfigToolStripMenuItem.Text = "Parser Config...";
            this.parserConfigToolStripMenuItem.Click += new System.EventHandler(this.parserConfigToolStripMenuItem_Click);
            // 
            // analyzeToolStripMenuItem
            // 
            this.analyzeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showMSNP2PConversationsToolStripMenuItem});
            this.analyzeToolStripMenuItem.Name = "analyzeToolStripMenuItem";
            this.analyzeToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
            this.analyzeToolStripMenuItem.Text = "&View";
            // 
            // showMSNP2PConversationsToolStripMenuItem
            // 
            this.showMSNP2PConversationsToolStripMenuItem.Name = "showMSNP2PConversationsToolStripMenuItem";
            this.showMSNP2PConversationsToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.showMSNP2PConversationsToolStripMenuItem.Text = "&Conversations...";
            this.showMSNP2PConversationsToolStripMenuItem.Click += new System.EventHandler(this.showMSNP2PConversationsToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.debugToolStripMenuItem1,
            this.toolStripMenuItem2,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(40, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // debugToolStripMenuItem1
            // 
            this.debugToolStripMenuItem1.Name = "debugToolStripMenuItem1";
            this.debugToolStripMenuItem1.Size = new System.Drawing.Size(116, 22);
            this.debugToolStripMenuItem1.Text = "&Debug";
            this.debugToolStripMenuItem1.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(113, 6);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // dataGridContextMenuStrip
            // 
            this.dataGridContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.goToReturnaddressInIDAToolStripMenuItem,
            this.toolStripMenuItem3,
            this.createSwRuleFromEntryToolStripMenuItem});
            this.dataGridContextMenuStrip.Name = "dataGridContextMenuStrip";
            this.dataGridContextMenuStrip.Size = new System.Drawing.Size(246, 54);
            this.dataGridContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.dataGridContextMenuStrip_Opening);
            // 
            // goToReturnaddressInIDAToolStripMenuItem
            // 
            this.goToReturnaddressInIDAToolStripMenuItem.Name = "goToReturnaddressInIDAToolStripMenuItem";
            this.goToReturnaddressInIDAToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.goToReturnaddressInIDAToolStripMenuItem.Text = "&Go to return address in IDA";
            this.goToReturnaddressInIDAToolStripMenuItem.Click += new System.EventHandler(this.goToReturnaddressInIDAToolStripMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(242, 6);
            // 
            // createSwRuleFromEntryToolStripMenuItem
            // 
            this.createSwRuleFromEntryToolStripMenuItem.Name = "createSwRuleFromEntryToolStripMenuItem";
            this.createSwRuleFromEntryToolStripMenuItem.Size = new System.Drawing.Size(245, 22);
            this.createSwRuleFromEntryToolStripMenuItem.Text = "&Create softwall rule from entry...";
            this.createSwRuleFromEntryToolStripMenuItem.Click += new System.EventHandler(this.createSwRuleFromEntryToolStripMenuItem_Click);
            // 
            // bindingSource
            // 
            this.bindingSource.DataMember = "Messages";
            this.bindingSource.DataSource = this.dataSet;
            this.bindingSource.Sort = "Index ASC";
            // 
            // dumpContextMenuStrip
            // 
            this.dumpContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.saveRawDumpToFileToolStripMenuItem,
            this.toolStripMenuItem4,
            this.showAsToolStripMenuItem});
            this.dumpContextMenuStrip.Name = "dumpContextMenuStrip";
            this.dumpContextMenuStrip.Size = new System.Drawing.Size(202, 76);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.copyToolStripMenuItem.Text = "&Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // saveRawDumpToFileToolStripMenuItem
            // 
            this.saveRawDumpToFileToolStripMenuItem.Name = "saveRawDumpToFileToolStripMenuItem";
            this.saveRawDumpToFileToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.saveRawDumpToFileToolStripMenuItem.Text = "&Save raw dump to file...";
            this.saveRawDumpToFileToolStripMenuItem.Click += new System.EventHandler(this.saveRawDumpToFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(198, 6);
            // 
            // showAsToolStripMenuItem
            // 
            this.showAsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aSCIIToolStripMenuItem,
            this.hexToolStripMenuItem});
            this.showAsToolStripMenuItem.Name = "showAsToolStripMenuItem";
            this.showAsToolStripMenuItem.Size = new System.Drawing.Size(201, 22);
            this.showAsToolStripMenuItem.Text = "&View as";
            // 
            // aSCIIToolStripMenuItem
            // 
            this.aSCIIToolStripMenuItem.Name = "aSCIIToolStripMenuItem";
            this.aSCIIToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.aSCIIToolStripMenuItem.Text = "&ASCII";
            this.aSCIIToolStripMenuItem.Click += new System.EventHandler(this.aSCIIToolStripMenuItem_Click);
            // 
            // hexToolStripMenuItem
            // 
            this.hexToolStripMenuItem.Checked = true;
            this.hexToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.hexToolStripMenuItem.Name = "hexToolStripMenuItem";
            this.hexToolStripMenuItem.Size = new System.Drawing.Size(113, 22);
            this.hexToolStripMenuItem.Text = "&Hex";
            this.hexToolStripMenuItem.Click += new System.EventHandler(this.hexToolStripMenuItem_Click);
            // 
            // propertyGridContextMenuStrip
            // 
            this.propertyGridContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.expandAllToolStripMenuItem,
            this.collapseAllToolStripMenuItem,
            this.toolStripMenuItem1,
            this.autoHighlightMenuItem});
            this.propertyGridContextMenuStrip.Name = "contextMenuStrip1";
            this.propertyGridContextMenuStrip.Size = new System.Drawing.Size(191, 76);
            // 
            // expandAllToolStripMenuItem
            // 
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.expandAllToolStripMenuItem.Text = "&Expand all";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // collapseAllToolStripMenuItem
            // 
            this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
            this.collapseAllToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.collapseAllToolStripMenuItem.Text = "&Collapse all";
            this.collapseAllToolStripMenuItem.Click += new System.EventHandler(this.collapseAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(187, 6);
            // 
            // autoHighlightMenuItem
            // 
            this.autoHighlightMenuItem.Checked = true;
            this.autoHighlightMenuItem.CheckOnClick = true;
            this.autoHighlightMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoHighlightMenuItem.Name = "autoHighlightMenuItem";
            this.autoHighlightMenuItem.Size = new System.Drawing.Size(190, 22);
            this.autoHighlightMenuItem.Text = "&Automatic highlighting";
            this.autoHighlightMenuItem.CheckedChanged += new System.EventHandler(this.autoHighlightMenuItem_CheckedChanged);
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.Location = new System.Drawing.Point(0, 52);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.dataGridView);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.splitContainer1);
            this.splitContainer.Size = new System.Drawing.Size(828, 510);
            this.splitContainer.SplitterDistance = 233;
            this.splitContainer.TabIndex = 8;
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToDeleteRows = false;
            this.dataGridView.AllowUserToResizeRows = false;
            this.dataGridView.AutoGenerateColumns = false;
            this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView.BackgroundColor = System.Drawing.SystemColors.ControlDark;
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.indexDataGridViewTextBoxColumn,
            this.typeDataGridViewImageColumn,
            this.timestampDataGridViewTextBoxColumn,
            this.processNameDataGridViewTextBoxColumn,
            this.processIdDataGridViewTextBoxColumn,
            this.threadIdDataGridViewTextBoxColumn,
            this.functionNameDataGridViewTextBoxColumn,
            this.returnAddressDataGridViewTextBoxColumn,
            this.callerModuleNameDataGridViewTextBoxColumn,
            this.resourceIdDataGridViewTextBoxColumn,
            this.msgTypeDataGridViewTextBoxColumn,
            this.msgContextDataGridViewTextBoxColumn,
            this.messageDataGridViewTextBoxColumn,
            this.directionDataGridViewTextBoxColumn,
            this.localAddressDataGridViewTextBoxColumn,
            this.localPortDataGridViewTextBoxColumn,
            this.peerAddressDataGridViewTextBoxColumn,
            this.peerPortDataGridViewTextBoxColumn,
            this.dataDataGridViewImageColumn,
            this.aSDeviceDataGridViewTextBoxColumn,
            this.aSStatusDataGridViewTextBoxColumn,
            this.aSSubStatusDataGridViewTextBoxColumn,
            this.aSWizStatusDataGridViewTextBoxColumn,
            this.senderDataGridViewTextBoxColumn,
            this.descriptionDataGridViewTextBoxColumn,
            this.Comment,
            this.bgColorDataGridViewTextBoxColumn});
            this.dataGridView.ContextMenuStrip = this.dataGridContextMenuStrip;
            this.dataGridView.DataSource = this.bindingSource;
            this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView.Location = new System.Drawing.Point(0, 0);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.ReadOnly = true;
            this.dataGridView.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(828, 233);
            this.dataGridView.TabIndex = 2;
            this.dataGridView.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.dataGridView_CellFormatting);
            this.dataGridView.SelectionChanged += new System.EventHandler(this.dataGridView_SelectionChanged);
            this.dataGridView.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.dataGridView_MouseDoubleClick);
            // 
            // indexDataGridViewTextBoxColumn
            // 
            this.indexDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.indexDataGridViewTextBoxColumn.DataPropertyName = "Index";
            this.indexDataGridViewTextBoxColumn.HeaderText = "Index";
            this.indexDataGridViewTextBoxColumn.Name = "indexDataGridViewTextBoxColumn";
            this.indexDataGridViewTextBoxColumn.ReadOnly = true;
            this.indexDataGridViewTextBoxColumn.Width = 58;
            // 
            // typeDataGridViewImageColumn
            // 
            this.typeDataGridViewImageColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
            this.typeDataGridViewImageColumn.HeaderText = "Type";
            this.typeDataGridViewImageColumn.Name = "typeDataGridViewImageColumn";
            this.typeDataGridViewImageColumn.ReadOnly = true;
            this.typeDataGridViewImageColumn.Width = 37;
            // 
            // timestampDataGridViewTextBoxColumn
            // 
            this.timestampDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.timestampDataGridViewTextBoxColumn.DataPropertyName = "Timestamp";
            dataGridViewCellStyle1.Format = "T";
            dataGridViewCellStyle1.NullValue = null;
            this.timestampDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle1;
            this.timestampDataGridViewTextBoxColumn.HeaderText = "Timestamp";
            this.timestampDataGridViewTextBoxColumn.Name = "timestampDataGridViewTextBoxColumn";
            this.timestampDataGridViewTextBoxColumn.ReadOnly = true;
            this.timestampDataGridViewTextBoxColumn.Width = 83;
            // 
            // processNameDataGridViewTextBoxColumn
            // 
            this.processNameDataGridViewTextBoxColumn.DataPropertyName = "ProcessName";
            this.processNameDataGridViewTextBoxColumn.HeaderText = "ProcessName";
            this.processNameDataGridViewTextBoxColumn.Name = "processNameDataGridViewTextBoxColumn";
            this.processNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.processNameDataGridViewTextBoxColumn.Visible = false;
            // 
            // processIdDataGridViewTextBoxColumn
            // 
            this.processIdDataGridViewTextBoxColumn.DataPropertyName = "ProcessId";
            this.processIdDataGridViewTextBoxColumn.HeaderText = "ProcessId";
            this.processIdDataGridViewTextBoxColumn.Name = "processIdDataGridViewTextBoxColumn";
            this.processIdDataGridViewTextBoxColumn.ReadOnly = true;
            this.processIdDataGridViewTextBoxColumn.Visible = false;
            // 
            // threadIdDataGridViewTextBoxColumn
            // 
            this.threadIdDataGridViewTextBoxColumn.DataPropertyName = "ThreadId";
            this.threadIdDataGridViewTextBoxColumn.HeaderText = "ThreadId";
            this.threadIdDataGridViewTextBoxColumn.Name = "threadIdDataGridViewTextBoxColumn";
            this.threadIdDataGridViewTextBoxColumn.ReadOnly = true;
            this.threadIdDataGridViewTextBoxColumn.Visible = false;
            // 
            // functionNameDataGridViewTextBoxColumn
            // 
            this.functionNameDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.functionNameDataGridViewTextBoxColumn.DataPropertyName = "FunctionName";
            this.functionNameDataGridViewTextBoxColumn.HeaderText = "FunctionName";
            this.functionNameDataGridViewTextBoxColumn.Name = "functionNameDataGridViewTextBoxColumn";
            this.functionNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.functionNameDataGridViewTextBoxColumn.Width = 101;
            // 
            // returnAddressDataGridViewTextBoxColumn
            // 
            this.returnAddressDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.returnAddressDataGridViewTextBoxColumn.DataPropertyName = "ReturnAddress";
            dataGridViewCellStyle2.Format = "x";
            dataGridViewCellStyle2.NullValue = null;
            this.returnAddressDataGridViewTextBoxColumn.DefaultCellStyle = dataGridViewCellStyle2;
            this.returnAddressDataGridViewTextBoxColumn.HeaderText = "ReturnAddress";
            this.returnAddressDataGridViewTextBoxColumn.Name = "returnAddressDataGridViewTextBoxColumn";
            this.returnAddressDataGridViewTextBoxColumn.ReadOnly = true;
            this.returnAddressDataGridViewTextBoxColumn.Width = 102;
            // 
            // callerModuleNameDataGridViewTextBoxColumn
            // 
            this.callerModuleNameDataGridViewTextBoxColumn.DataPropertyName = "CallerModuleName";
            this.callerModuleNameDataGridViewTextBoxColumn.HeaderText = "CallerModuleName";
            this.callerModuleNameDataGridViewTextBoxColumn.Name = "callerModuleNameDataGridViewTextBoxColumn";
            this.callerModuleNameDataGridViewTextBoxColumn.ReadOnly = true;
            this.callerModuleNameDataGridViewTextBoxColumn.Visible = false;
            // 
            // resourceIdDataGridViewTextBoxColumn
            // 
            this.resourceIdDataGridViewTextBoxColumn.DataPropertyName = "ResourceId";
            this.resourceIdDataGridViewTextBoxColumn.HeaderText = "ResourceId";
            this.resourceIdDataGridViewTextBoxColumn.Name = "resourceIdDataGridViewTextBoxColumn";
            this.resourceIdDataGridViewTextBoxColumn.ReadOnly = true;
            this.resourceIdDataGridViewTextBoxColumn.Visible = false;
            // 
            // msgTypeDataGridViewTextBoxColumn
            // 
            this.msgTypeDataGridViewTextBoxColumn.DataPropertyName = "MsgType";
            this.msgTypeDataGridViewTextBoxColumn.HeaderText = "MsgType";
            this.msgTypeDataGridViewTextBoxColumn.Name = "msgTypeDataGridViewTextBoxColumn";
            this.msgTypeDataGridViewTextBoxColumn.ReadOnly = true;
            this.msgTypeDataGridViewTextBoxColumn.Visible = false;
            // 
            // msgContextDataGridViewTextBoxColumn
            // 
            this.msgContextDataGridViewTextBoxColumn.DataPropertyName = "MsgContext";
            this.msgContextDataGridViewTextBoxColumn.HeaderText = "MsgContext";
            this.msgContextDataGridViewTextBoxColumn.Name = "msgContextDataGridViewTextBoxColumn";
            this.msgContextDataGridViewTextBoxColumn.ReadOnly = true;
            this.msgContextDataGridViewTextBoxColumn.Visible = false;
            // 
            // messageDataGridViewTextBoxColumn
            // 
            this.messageDataGridViewTextBoxColumn.DataPropertyName = "Message";
            this.messageDataGridViewTextBoxColumn.HeaderText = "Message";
            this.messageDataGridViewTextBoxColumn.Name = "messageDataGridViewTextBoxColumn";
            this.messageDataGridViewTextBoxColumn.ReadOnly = true;
            this.messageDataGridViewTextBoxColumn.Visible = false;
            // 
            // directionDataGridViewTextBoxColumn
            // 
            this.directionDataGridViewTextBoxColumn.DataPropertyName = "Direction";
            this.directionDataGridViewTextBoxColumn.HeaderText = "Direction";
            this.directionDataGridViewTextBoxColumn.Name = "directionDataGridViewTextBoxColumn";
            this.directionDataGridViewTextBoxColumn.ReadOnly = true;
            this.directionDataGridViewTextBoxColumn.Visible = false;
            // 
            // localAddressDataGridViewTextBoxColumn
            // 
            this.localAddressDataGridViewTextBoxColumn.DataPropertyName = "LocalAddress";
            this.localAddressDataGridViewTextBoxColumn.HeaderText = "LocalAddress";
            this.localAddressDataGridViewTextBoxColumn.Name = "localAddressDataGridViewTextBoxColumn";
            this.localAddressDataGridViewTextBoxColumn.ReadOnly = true;
            this.localAddressDataGridViewTextBoxColumn.Visible = false;
            // 
            // localPortDataGridViewTextBoxColumn
            // 
            this.localPortDataGridViewTextBoxColumn.DataPropertyName = "LocalPort";
            this.localPortDataGridViewTextBoxColumn.HeaderText = "LocalPort";
            this.localPortDataGridViewTextBoxColumn.Name = "localPortDataGridViewTextBoxColumn";
            this.localPortDataGridViewTextBoxColumn.ReadOnly = true;
            this.localPortDataGridViewTextBoxColumn.Visible = false;
            // 
            // peerAddressDataGridViewTextBoxColumn
            // 
            this.peerAddressDataGridViewTextBoxColumn.DataPropertyName = "PeerAddress";
            this.peerAddressDataGridViewTextBoxColumn.HeaderText = "PeerAddress";
            this.peerAddressDataGridViewTextBoxColumn.Name = "peerAddressDataGridViewTextBoxColumn";
            this.peerAddressDataGridViewTextBoxColumn.ReadOnly = true;
            this.peerAddressDataGridViewTextBoxColumn.Visible = false;
            // 
            // peerPortDataGridViewTextBoxColumn
            // 
            this.peerPortDataGridViewTextBoxColumn.DataPropertyName = "PeerPort";
            this.peerPortDataGridViewTextBoxColumn.HeaderText = "PeerPort";
            this.peerPortDataGridViewTextBoxColumn.Name = "peerPortDataGridViewTextBoxColumn";
            this.peerPortDataGridViewTextBoxColumn.ReadOnly = true;
            this.peerPortDataGridViewTextBoxColumn.Visible = false;
            // 
            // dataDataGridViewImageColumn
            // 
            this.dataDataGridViewImageColumn.DataPropertyName = "Data";
            this.dataDataGridViewImageColumn.HeaderText = "Data";
            this.dataDataGridViewImageColumn.Name = "dataDataGridViewImageColumn";
            this.dataDataGridViewImageColumn.ReadOnly = true;
            this.dataDataGridViewImageColumn.Visible = false;
            // 
            // aSDeviceDataGridViewTextBoxColumn
            // 
            this.aSDeviceDataGridViewTextBoxColumn.DataPropertyName = "AS_Device";
            this.aSDeviceDataGridViewTextBoxColumn.HeaderText = "AS_Device";
            this.aSDeviceDataGridViewTextBoxColumn.Name = "aSDeviceDataGridViewTextBoxColumn";
            this.aSDeviceDataGridViewTextBoxColumn.ReadOnly = true;
            this.aSDeviceDataGridViewTextBoxColumn.Visible = false;
            // 
            // aSStatusDataGridViewTextBoxColumn
            // 
            this.aSStatusDataGridViewTextBoxColumn.DataPropertyName = "AS_Status";
            this.aSStatusDataGridViewTextBoxColumn.HeaderText = "AS_Status";
            this.aSStatusDataGridViewTextBoxColumn.Name = "aSStatusDataGridViewTextBoxColumn";
            this.aSStatusDataGridViewTextBoxColumn.ReadOnly = true;
            this.aSStatusDataGridViewTextBoxColumn.Visible = false;
            // 
            // aSSubStatusDataGridViewTextBoxColumn
            // 
            this.aSSubStatusDataGridViewTextBoxColumn.DataPropertyName = "AS_SubStatus";
            this.aSSubStatusDataGridViewTextBoxColumn.HeaderText = "AS_SubStatus";
            this.aSSubStatusDataGridViewTextBoxColumn.Name = "aSSubStatusDataGridViewTextBoxColumn";
            this.aSSubStatusDataGridViewTextBoxColumn.ReadOnly = true;
            this.aSSubStatusDataGridViewTextBoxColumn.Visible = false;
            // 
            // aSWizStatusDataGridViewTextBoxColumn
            // 
            this.aSWizStatusDataGridViewTextBoxColumn.DataPropertyName = "AS_WizStatus";
            this.aSWizStatusDataGridViewTextBoxColumn.HeaderText = "AS_WizStatus";
            this.aSWizStatusDataGridViewTextBoxColumn.Name = "aSWizStatusDataGridViewTextBoxColumn";
            this.aSWizStatusDataGridViewTextBoxColumn.ReadOnly = true;
            this.aSWizStatusDataGridViewTextBoxColumn.Visible = false;
            // 
            // senderDataGridViewTextBoxColumn
            // 
            this.senderDataGridViewTextBoxColumn.DataPropertyName = "Sender";
            this.senderDataGridViewTextBoxColumn.HeaderText = "Sender";
            this.senderDataGridViewTextBoxColumn.Name = "senderDataGridViewTextBoxColumn";
            this.senderDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // descriptionDataGridViewTextBoxColumn
            // 
            this.descriptionDataGridViewTextBoxColumn.DataPropertyName = "Description";
            this.descriptionDataGridViewTextBoxColumn.HeaderText = "Description";
            this.descriptionDataGridViewTextBoxColumn.Name = "descriptionDataGridViewTextBoxColumn";
            this.descriptionDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // Comment
            // 
            this.Comment.DataPropertyName = "Comment";
            this.Comment.HeaderText = "Comment";
            this.Comment.Name = "Comment";
            this.Comment.ReadOnly = true;
            // 
            // bgColorDataGridViewTextBoxColumn
            // 
            this.bgColorDataGridViewTextBoxColumn.DataPropertyName = "BgColor";
            this.bgColorDataGridViewTextBoxColumn.HeaderText = "BgColor";
            this.bgColorDataGridViewTextBoxColumn.Name = "bgColorDataGridViewTextBoxColumn";
            this.bgColorDataGridViewTextBoxColumn.ReadOnly = true;
            this.bgColorDataGridViewTextBoxColumn.Visible = false;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.richTextBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.propertyGrid);
            this.splitContainer1.Size = new System.Drawing.Size(828, 273);
            this.splitContainer1.SplitterDistance = 623;
            this.splitContainer1.TabIndex = 0;
            // 
            // richTextBox
            // 
            this.richTextBox.BackColor = System.Drawing.Color.Gray;
            this.richTextBox.ContextMenuStrip = this.dumpContextMenuStrip;
            this.richTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox.Font = new System.Drawing.Font("Lucida Console", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox.ForeColor = System.Drawing.Color.Black;
            this.richTextBox.Location = new System.Drawing.Point(0, 0);
            this.richTextBox.Name = "richTextBox";
            this.richTextBox.ReadOnly = true;
            this.richTextBox.Size = new System.Drawing.Size(623, 273);
            this.richTextBox.TabIndex = 0;
            this.richTextBox.Text = "";
            this.richTextBox.WordWrap = false;
            // 
            // propertyGrid
            // 
            this.propertyGrid.BackColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.CommandsBackColor = System.Drawing.SystemColors.Control;
            this.propertyGrid.ContextMenuStrip = this.propertyGridContextMenuStrip;
            this.propertyGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid.Name = "propertyGrid";
            this.propertyGrid.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.propertyGrid.Size = new System.Drawing.Size(201, 273);
            this.propertyGrid.TabIndex = 2;
            this.propertyGrid.ToolbarVisible = false;
            this.propertyGrid.SelectedGridItemChanged += new System.Windows.Forms.SelectedGridItemChangedEventHandler(this.propertyGrid_SelectedGridItemChanged);
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.filterComboBox,
            this.toolStripSeparator3,
            this.toolStripLabel2,
            this.findTypeComboBox,
            this.findComboBox});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(828, 25);
            this.toolStrip1.TabIndex = 9;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(35, 22);
            this.toolStripLabel1.Text = "Filter:";
            // 
            // filterComboBox
            // 
            this.filterComboBox.Items.AddRange(new object[] {
            "(NOT FunctionName = \'RAPIStubDebug\') AND (NOT FunctionName = \'RAPIMgrDebug\')",
            resources.GetString("filterComboBox.Items"),
            "LocalPort = 990",
            "LocalPort = 999",
            "LocalPort = 5678",
            "LocalPort = 26675",
            "LocalPort = 990 OR LocalPort = 999 OR LocalPort = 5678 OR LocalPort = 26675",
            "PeerAddress = \'10.0.0.15\'"});
            this.filterComboBox.Name = "filterComboBox";
            this.filterComboBox.Size = new System.Drawing.Size(300, 25);
            this.filterComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.filterComboBox_KeyDown);
            this.filterComboBox.SelectedIndexChanged += new System.EventHandler(this.filterComboBox_SelectedIndexChanged);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(31, 22);
            this.toolStripLabel2.Text = "Find:";
            // 
            // findTypeComboBox
            // 
            this.findTypeComboBox.AutoSize = false;
            this.findTypeComboBox.Items.AddRange(new object[] {
            "ASCII string",
            "ASCII string, case-insensitive",
            "UTF16 LE string",
            "UTF16 LE string, case-insensitive",
            "HEX byte sequence"});
            this.findTypeComboBox.Name = "findTypeComboBox";
            this.findTypeComboBox.Size = new System.Drawing.Size(180, 21);
            this.findTypeComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.findTypeComboBox_KeyDown);
            // 
            // findComboBox
            // 
            this.findComboBox.AutoSize = false;
            this.findComboBox.Name = "findComboBox";
            this.findComboBox.Size = new System.Drawing.Size(200, 21);
            this.findComboBox.Leave += new System.EventHandler(this.findComboBox_Leave);
            this.findComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.findComboBox_KeyDown);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusBarLabel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 563);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(828, 22);
            this.statusStrip1.TabIndex = 10;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // statusBarLabel
            // 
            this.statusBarLabel.Name = "statusBarLabel";
            this.statusBarLabel.Size = new System.Drawing.Size(0, 17);
            // 
            // dumpSaveFileDialog
            // 
            this.dumpSaveFileDialog.DefaultExt = "bin";
            this.dumpSaveFileDialog.Filter = "Binary dump files|*.bin";
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(828, 585);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "oSpy";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.messageTbl)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.dataGridContextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            this.dumpContextMenuStrip.ResumeLayout(false);
            this.propertyGridContextMenuStrip.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Data.DataSet dataSet;
        private System.Data.DataTable messageTbl;
        private System.Data.DataColumn indexCol;
        private System.Data.DataColumn timestampCol;
        private System.Data.DataColumn functionNameCol;
        private System.Data.DataColumn processIdCol;
        private System.Data.DataColumn threadIdCol;
        private System.Data.DataColumn localAddressCol;
        private System.Data.DataColumn localPortCol;
        private System.Data.DataColumn peerAddressCol;
        private System.Data.DataColumn peerPortCol;
        private System.Data.DataColumn dataCol;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem captureToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem captureStartMenuItem;
        private System.Data.DataColumn processNameCol;
        private System.Data.DataColumn returnAddressCol;
        private System.Windows.Forms.ToolStripMenuItem injectToolStripMenuItem;
        private System.Data.DataColumn msgTypeCol;
        private System.Data.DataColumn messageCol;
        private System.Data.DataColumn contextCol;
        private System.Data.DataColumn directionCol;
        private System.Windows.Forms.ToolStripMenuItem clearMenuItem;
        private System.Data.DataColumn ASDeviceCol;
        private System.Data.DataColumn ASStatusCol;
        private System.Data.DataColumn ASSubStatusCol;
        private System.Data.DataColumn ASWizStatusCol;
        private System.Windows.Forms.BindingSource bindingSource;
        private System.Data.DataColumn senderCol;
        private System.Data.DataColumn descriptionCol;
        private System.Data.DataColumn bgColorCol;
        private System.Data.DataColumn commentCol;
        private System.Windows.Forms.ContextMenuStrip propertyGridContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem expandAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem autoHighlightMenuItem;
        private System.Data.DataColumn callerModuleNameCol;
        private System.Windows.Forms.ContextMenuStrip dumpContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem saveRawDumpToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem analyzeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMSNP2PConversationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Data.DataColumn resourceIdCol;
        private System.Windows.Forms.ContextMenuStrip dataGridContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem createSwRuleFromEntryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem manageSoftwallRulesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aSCIIToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hexToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        protected System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.RichTextBox richTextBox;
        private System.Windows.Forms.PropertyGrid propertyGrid;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripComboBox filterComboBox;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripComboBox findTypeComboBox;
        private System.Windows.Forms.ToolStripComboBox findComboBox;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel statusBarLabel;
        private System.Windows.Forms.DataGridViewTextBoxColumn indexDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewImageColumn typeDataGridViewImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn timestampDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn processNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn processIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn threadIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn functionNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn returnAddressDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn callerModuleNameDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn resourceIdDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn msgTypeDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn msgContextDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn messageDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn directionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn localAddressDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn localPortDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn peerAddressDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn peerPortDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewImageColumn dataDataGridViewImageColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aSDeviceDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aSStatusDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aSSubStatusDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn aSWizStatusDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn senderDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn descriptionDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Comment;
        private System.Windows.Forms.DataGridViewTextBoxColumn bgColorDataGridViewTextBoxColumn;
        private System.Windows.Forms.SaveFileDialog dumpSaveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem goToReturnaddressInIDAToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem parserConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectAlltransactionsToolStripMenuItem;
    }
}

