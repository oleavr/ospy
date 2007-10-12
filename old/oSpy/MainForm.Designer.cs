//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.dataSet = new System.Data.DataSet();
            this.eventsTbl = new System.Data.DataTable();
            this.eventCol = new System.Data.DataColumn();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newCaptureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.clearMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.rowsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.transactionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.goToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextpacketToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.nextRowTransactionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.manageSoftwallRulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.parserConfigToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.analyzeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewInternalDebugToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewWinCryptToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripSeparator();
            this.showMSNP2PConversationsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.applydebugSymbolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tryggveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.debugToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.goToReturnaddressInIDAToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.showbacktraceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripSeparator();
            this.createSwRuleFromEntryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dumpContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selBytesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copyRawToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selSaveToFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripSeparator();
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
            this.mainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.lowerSplitContainer = new System.Windows.Forms.SplitContainer();
            this.richTextBox = new System.Windows.Forms.RichTextBox();
            this.propertyGrid = new System.Windows.Forms.PropertyGrid();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.filterComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.findTypeComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.findComboBox = new System.Windows.Forms.ToolStripComboBox();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.nextPacketBtn = new System.Windows.Forms.ToolStripButton();
            this.nextTransactionBtn = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.statusBarLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.dumpSaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.eventTextCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.indexTextCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timestampTextCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.typeTextCol = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventsTbl)).BeginInit();
            this.menuStrip.SuspendLayout();
            this.dataGridContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).BeginInit();
            this.dumpContextMenuStrip.SuspendLayout();
            this.propertyGridContextMenuStrip.SuspendLayout();
            this.mainSplitContainer.Panel1.SuspendLayout();
            this.mainSplitContainer.Panel2.SuspendLayout();
            this.mainSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.lowerSplitContainer.Panel1.SuspendLayout();
            this.lowerSplitContainer.Panel2.SuspendLayout();
            this.lowerSplitContainer.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dataSet
            // 
            this.dataSet.CaseSensitive = true;
            this.dataSet.DataSetName = "oSpyDataSet";
            this.dataSet.Tables.AddRange(new System.Data.DataTable[] {
            this.eventsTbl});
            // 
            // eventsTbl
            // 
            this.eventsTbl.Columns.AddRange(new System.Data.DataColumn[] {
            this.eventCol});
            this.eventsTbl.TableName = "Events";
            // 
            // eventCol
            // 
            this.eventCol.AllowDBNull = false;
            this.eventCol.ColumnName = "Event";
            this.eventCol.DataType = typeof(object);
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
            this.goToolStripMenuItem,
            this.optionsToolStripMenuItem,
            this.analyzeToolStripMenuItem,
            this.toolsToolStripMenuItem,
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
            this.clearMenuItem,
            this.toolStripSeparator2,
            this.exitMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // newCaptureToolStripMenuItem
            // 
            this.newCaptureToolStripMenuItem.Name = "newCaptureToolStripMenuItem";
            this.newCaptureToolStripMenuItem.Size = new System.Drawing.Size(150, 22);
            this.newCaptureToolStripMenuItem.Text = "&New capture...";
            this.newCaptureToolStripMenuItem.Click += new System.EventHandler(this.newCaptureToolStripMenuItem_Click);
            // 
            // openMenuItem
            // 
            this.openMenuItem.Name = "openMenuItem";
            this.openMenuItem.Size = new System.Drawing.Size(150, 22);
            this.openMenuItem.Text = "&Open...";
            this.openMenuItem.Click += new System.EventHandler(this.openMenuItem_Click);
            // 
            // saveMenuItem
            // 
            this.saveMenuItem.Name = "saveMenuItem";
            this.saveMenuItem.Size = new System.Drawing.Size(150, 22);
            this.saveMenuItem.Text = "&Save...";
            this.saveMenuItem.Click += new System.EventHandler(this.saveMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(147, 6);
            // 
            // clearMenuItem
            // 
            this.clearMenuItem.Name = "clearMenuItem";
            this.clearMenuItem.Size = new System.Drawing.Size(150, 22);
            this.clearMenuItem.Text = "&Clear";
            this.clearMenuItem.Click += new System.EventHandler(this.clearMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(147, 6);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(150, 22);
            this.exitMenuItem.Text = "E&xit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.selectallToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
            this.editToolStripMenuItem.Text = "&Edit";
            // 
            // selectallToolStripMenuItem
            // 
            this.selectallToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.rowsToolStripMenuItem,
            this.transactionsToolStripMenuItem});
            this.selectallToolStripMenuItem.Name = "selectallToolStripMenuItem";
            this.selectallToolStripMenuItem.Size = new System.Drawing.Size(120, 22);
            this.selectallToolStripMenuItem.Text = "Select &all";
            // 
            // rowsToolStripMenuItem
            // 
            this.rowsToolStripMenuItem.Name = "rowsToolStripMenuItem";
            this.rowsToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.rowsToolStripMenuItem.Text = "&Rows";
            this.rowsToolStripMenuItem.Click += new System.EventHandler(this.rowsToolStripMenuItem_Click);
            // 
            // transactionsToolStripMenuItem
            // 
            this.transactionsToolStripMenuItem.Name = "transactionsToolStripMenuItem";
            this.transactionsToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            this.transactionsToolStripMenuItem.Text = "&Transactions";
            this.transactionsToolStripMenuItem.Click += new System.EventHandler(this.transactionsToolStripMenuItem_Click);
            // 
            // goToolStripMenuItem
            // 
            this.goToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.nextpacketToolStripMenuItem,
            this.nextRowTransactionToolStripMenuItem});
            this.goToolStripMenuItem.Name = "goToolStripMenuItem";
            this.goToolStripMenuItem.Size = new System.Drawing.Size(34, 20);
            this.goToolStripMenuItem.Text = "&Go";
            // 
            // nextpacketToolStripMenuItem
            // 
            this.nextpacketToolStripMenuItem.Name = "nextpacketToolStripMenuItem";
            this.nextpacketToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.nextpacketToolStripMenuItem.Size = new System.Drawing.Size(306, 22);
            this.nextpacketToolStripMenuItem.Text = "Next &packet row";
            this.nextpacketToolStripMenuItem.Click += new System.EventHandler(this.nextpacketToolStripMenuItem_Click);
            // 
            // nextRowTransactionToolStripMenuItem
            // 
            this.nextRowTransactionToolStripMenuItem.Name = "nextRowTransactionToolStripMenuItem";
            this.nextRowTransactionToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.N)));
            this.nextRowTransactionToolStripMenuItem.Size = new System.Drawing.Size(306, 22);
            this.nextRowTransactionToolStripMenuItem.Text = "Next row belonging to a &transaction";
            this.nextRowTransactionToolStripMenuItem.Click += new System.EventHandler(this.nextRowTransactionToolStripMenuItem_Click);
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.manageSoftwallRulesToolStripMenuItem,
            this.parserConfigToolStripMenuItem});
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(61, 20);
            this.optionsToolStripMenuItem.Text = "&Options";
            // 
            // manageSoftwallRulesToolStripMenuItem
            // 
            this.manageSoftwallRulesToolStripMenuItem.Name = "manageSoftwallRulesToolStripMenuItem";
            this.manageSoftwallRulesToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.manageSoftwallRulesToolStripMenuItem.Text = "&Softwall rules...";
            this.manageSoftwallRulesToolStripMenuItem.Click += new System.EventHandler(this.manageSoftwallRulesToolStripMenuItem_Click);
            // 
            // parserConfigToolStripMenuItem
            // 
            this.parserConfigToolStripMenuItem.Name = "parserConfigToolStripMenuItem";
            this.parserConfigToolStripMenuItem.Size = new System.Drawing.Size(154, 22);
            this.parserConfigToolStripMenuItem.Text = "Parser Config...";
            this.parserConfigToolStripMenuItem.Click += new System.EventHandler(this.parserConfigToolStripMenuItem_Click);
            // 
            // analyzeToolStripMenuItem
            // 
            this.analyzeToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.viewInternalDebugToolStripMenuItem,
            this.viewWinCryptToolStripMenuItem,
            this.toolStripMenuItem5,
            this.showMSNP2PConversationsToolStripMenuItem});
            this.analyzeToolStripMenuItem.Name = "analyzeToolStripMenuItem";
            this.analyzeToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.analyzeToolStripMenuItem.Text = "&View";
            // 
            // viewInternalDebugToolStripMenuItem
            // 
            this.viewInternalDebugToolStripMenuItem.Checked = true;
            this.viewInternalDebugToolStripMenuItem.CheckOnClick = true;
            this.viewInternalDebugToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.viewInternalDebugToolStripMenuItem.Name = "viewInternalDebugToolStripMenuItem";
            this.viewInternalDebugToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.viewInternalDebugToolStripMenuItem.Text = "Internal debug";
            this.viewInternalDebugToolStripMenuItem.CheckedChanged += new System.EventHandler(this.viewInternalDebugToolStripMenuItem_CheckedChanged);
            // 
            // viewWinCryptToolStripMenuItem
            // 
            this.viewWinCryptToolStripMenuItem.Checked = true;
            this.viewWinCryptToolStripMenuItem.CheckOnClick = true;
            this.viewWinCryptToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.viewWinCryptToolStripMenuItem.Name = "viewWinCryptToolStripMenuItem";
            this.viewWinCryptToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.viewWinCryptToolStripMenuItem.Text = "WinCrypt API";
            this.viewWinCryptToolStripMenuItem.CheckedChanged += new System.EventHandler(this.viewWinCryptToolStripMenuItem_CheckedChanged);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(155, 6);
            // 
            // showMSNP2PConversationsToolStripMenuItem
            // 
            this.showMSNP2PConversationsToolStripMenuItem.Name = "showMSNP2PConversationsToolStripMenuItem";
            this.showMSNP2PConversationsToolStripMenuItem.Size = new System.Drawing.Size(158, 22);
            this.showMSNP2PConversationsToolStripMenuItem.Text = "&Conversations...";
            this.showMSNP2PConversationsToolStripMenuItem.Click += new System.EventHandler(this.showMSNP2PConversationsToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.applydebugSymbolsToolStripMenuItem,
            this.tryggveToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.toolsToolStripMenuItem.Text = "&Tools";
            // 
            // applydebugSymbolsToolStripMenuItem
            // 
            this.applydebugSymbolsToolStripMenuItem.Name = "applydebugSymbolsToolStripMenuItem";
            this.applydebugSymbolsToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.applydebugSymbolsToolStripMenuItem.Text = "Apply &debug symbols";
            this.applydebugSymbolsToolStripMenuItem.Click += new System.EventHandler(this.applydebugSymbolsToolStripMenuItem_Click);
            // 
            // tryggveToolStripMenuItem
            // 
            this.tryggveToolStripMenuItem.Name = "tryggveToolStripMenuItem";
            this.tryggveToolStripMenuItem.Size = new System.Drawing.Size(189, 22);
            this.tryggveToolStripMenuItem.Text = "&Tryggve";
            this.tryggveToolStripMenuItem.Click += new System.EventHandler(this.tryggveToolStripMenuItem_Click);
            // 
            // helpToolStripMenuItem
            // 
            this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.debugToolStripMenuItem1,
            this.toolStripMenuItem2,
            this.aboutToolStripMenuItem});
            this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            this.helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.helpToolStripMenuItem.Text = "&Help";
            // 
            // debugToolStripMenuItem1
            // 
            this.debugToolStripMenuItem1.Name = "debugToolStripMenuItem1";
            this.debugToolStripMenuItem1.Size = new System.Drawing.Size(109, 22);
            this.debugToolStripMenuItem1.Text = "&Debug";
            this.debugToolStripMenuItem1.Click += new System.EventHandler(this.debugToolStripMenuItem_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(106, 6);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.aboutToolStripMenuItem.Text = "&About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // dataGridContextMenuStrip
            // 
            this.dataGridContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.goToReturnaddressInIDAToolStripMenuItem,
            this.showbacktraceToolStripMenuItem,
            this.toolStripMenuItem3,
            this.createSwRuleFromEntryToolStripMenuItem});
            this.dataGridContextMenuStrip.Name = "dataGridContextMenuStrip";
            this.dataGridContextMenuStrip.Size = new System.Drawing.Size(244, 76);
            this.dataGridContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.dataGridContextMenuStrip_Opening);
            // 
            // goToReturnaddressInIDAToolStripMenuItem
            // 
            this.goToReturnaddressInIDAToolStripMenuItem.Name = "goToReturnaddressInIDAToolStripMenuItem";
            this.goToReturnaddressInIDAToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.goToReturnaddressInIDAToolStripMenuItem.Text = "&Go to return address in IDA";
            this.goToReturnaddressInIDAToolStripMenuItem.Click += new System.EventHandler(this.goToReturnaddressInIDAToolStripMenuItem_Click);
            // 
            // showbacktraceToolStripMenuItem
            // 
            this.showbacktraceToolStripMenuItem.Name = "showbacktraceToolStripMenuItem";
            this.showbacktraceToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.showbacktraceToolStripMenuItem.Text = "Show &backtrace...";
            this.showbacktraceToolStripMenuItem.Click += new System.EventHandler(this.showbacktraceToolStripMenuItem_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(240, 6);
            // 
            // createSwRuleFromEntryToolStripMenuItem
            // 
            this.createSwRuleFromEntryToolStripMenuItem.Name = "createSwRuleFromEntryToolStripMenuItem";
            this.createSwRuleFromEntryToolStripMenuItem.Size = new System.Drawing.Size(243, 22);
            this.createSwRuleFromEntryToolStripMenuItem.Text = "&Create softwall rule from entry...";
            this.createSwRuleFromEntryToolStripMenuItem.Click += new System.EventHandler(this.createSwRuleFromEntryToolStripMenuItem_Click);
            // 
            // bindingSource
            // 
            this.bindingSource.DataMember = "Events";
            this.bindingSource.DataSource = this.dataSet;
            this.bindingSource.Sort = "";
            // 
            // dumpContextMenuStrip
            // 
            this.dumpContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.selBytesToolStripMenuItem,
            this.toolStripMenuItem6,
            this.saveRawDumpToFileToolStripMenuItem,
            this.toolStripMenuItem4,
            this.showAsToolStripMenuItem});
            this.dumpContextMenuStrip.Name = "dumpContextMenuStrip";
            this.dumpContextMenuStrip.Size = new System.Drawing.Size(198, 104);
            this.dumpContextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.dumpContextMenuStrip_Opening);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.copyToolStripMenuItem.Text = "&Copy";
            this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
            // 
            // selBytesToolStripMenuItem
            // 
            this.selBytesToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyRawToolStripMenuItem,
            this.selSaveToFileToolStripMenuItem});
            this.selBytesToolStripMenuItem.Name = "selBytesToolStripMenuItem";
            this.selBytesToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.selBytesToolStripMenuItem.Text = "Selected &bytes";
            // 
            // copyRawToolStripMenuItem
            // 
            this.copyRawToolStripMenuItem.Name = "copyRawToolStripMenuItem";
            this.copyRawToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.copyRawToolStripMenuItem.Text = "&Copy";
            this.copyRawToolStripMenuItem.Click += new System.EventHandler(this.copyRawToolStripMenuItem_Click);
            // 
            // selSaveToFileToolStripMenuItem
            // 
            this.selSaveToFileToolStripMenuItem.Name = "selSaveToFileToolStripMenuItem";
            this.selSaveToFileToolStripMenuItem.Size = new System.Drawing.Size(140, 22);
            this.selSaveToFileToolStripMenuItem.Text = "Save to &file...";
            this.selSaveToFileToolStripMenuItem.Click += new System.EventHandler(this.selSaveToFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(194, 6);
            // 
            // saveRawDumpToFileToolStripMenuItem
            // 
            this.saveRawDumpToFileToolStripMenuItem.Name = "saveRawDumpToFileToolStripMenuItem";
            this.saveRawDumpToFileToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.saveRawDumpToFileToolStripMenuItem.Text = "Save raw dump to &file...";
            this.saveRawDumpToFileToolStripMenuItem.Click += new System.EventHandler(this.saveRawDumpToFileToolStripMenuItem_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(194, 6);
            // 
            // showAsToolStripMenuItem
            // 
            this.showAsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.aSCIIToolStripMenuItem,
            this.hexToolStripMenuItem});
            this.showAsToolStripMenuItem.Name = "showAsToolStripMenuItem";
            this.showAsToolStripMenuItem.Size = new System.Drawing.Size(197, 22);
            this.showAsToolStripMenuItem.Text = "&View as";
            // 
            // aSCIIToolStripMenuItem
            // 
            this.aSCIIToolStripMenuItem.Name = "aSCIIToolStripMenuItem";
            this.aSCIIToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
            this.aSCIIToolStripMenuItem.Text = "&ASCII";
            this.aSCIIToolStripMenuItem.Click += new System.EventHandler(this.aSCIIToolStripMenuItem_Click);
            // 
            // hexToolStripMenuItem
            // 
            this.hexToolStripMenuItem.Checked = true;
            this.hexToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.hexToolStripMenuItem.Name = "hexToolStripMenuItem";
            this.hexToolStripMenuItem.Size = new System.Drawing.Size(102, 22);
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
            this.propertyGridContextMenuStrip.Size = new System.Drawing.Size(199, 76);
            // 
            // expandAllToolStripMenuItem
            // 
            this.expandAllToolStripMenuItem.Name = "expandAllToolStripMenuItem";
            this.expandAllToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.expandAllToolStripMenuItem.Text = "&Expand all";
            this.expandAllToolStripMenuItem.Click += new System.EventHandler(this.expandAllToolStripMenuItem_Click);
            // 
            // collapseAllToolStripMenuItem
            // 
            this.collapseAllToolStripMenuItem.Name = "collapseAllToolStripMenuItem";
            this.collapseAllToolStripMenuItem.Size = new System.Drawing.Size(198, 22);
            this.collapseAllToolStripMenuItem.Text = "&Collapse all";
            this.collapseAllToolStripMenuItem.Click += new System.EventHandler(this.collapseAllToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(195, 6);
            // 
            // autoHighlightMenuItem
            // 
            this.autoHighlightMenuItem.Checked = true;
            this.autoHighlightMenuItem.CheckOnClick = true;
            this.autoHighlightMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
            this.autoHighlightMenuItem.Name = "autoHighlightMenuItem";
            this.autoHighlightMenuItem.Size = new System.Drawing.Size(198, 22);
            this.autoHighlightMenuItem.Text = "&Automatic highlighting";
            this.autoHighlightMenuItem.CheckedChanged += new System.EventHandler(this.autoHighlightMenuItem_CheckedChanged);
            // 
            // mainSplitContainer
            // 
            this.mainSplitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.mainSplitContainer.Location = new System.Drawing.Point(0, 52);
            this.mainSplitContainer.Name = "mainSplitContainer";
            this.mainSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // mainSplitContainer.Panel1
            // 
            this.mainSplitContainer.Panel1.Controls.Add(this.dataGridView);
            // 
            // mainSplitContainer.Panel2
            // 
            this.mainSplitContainer.Panel2.Controls.Add(this.lowerSplitContainer);
            this.mainSplitContainer.Size = new System.Drawing.Size(828, 510);
            this.mainSplitContainer.SplitterDistance = 233;
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
            this.eventTextCol,
            this.indexTextCol,
            this.timestampTextCol,
            this.typeTextCol});
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
            // lowerSplitContainer
            // 
            this.lowerSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lowerSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.lowerSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.lowerSplitContainer.Name = "lowerSplitContainer";
            // 
            // lowerSplitContainer.Panel1
            // 
            this.lowerSplitContainer.Panel1.Controls.Add(this.richTextBox);
            // 
            // lowerSplitContainer.Panel2
            // 
            this.lowerSplitContainer.Panel2.Controls.Add(this.propertyGrid);
            this.lowerSplitContainer.Size = new System.Drawing.Size(828, 273);
            this.lowerSplitContainer.SplitterDistance = 623;
            this.lowerSplitContainer.TabIndex = 0;
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
            this.richTextBox.SelectionChanged += new System.EventHandler(this.richTextBox_SelectionChanged);
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
            this.findComboBox,
            this.toolStripSeparator4,
            this.nextPacketBtn,
            this.nextTransactionBtn});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(828, 25);
            this.toolStrip1.TabIndex = 9;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(36, 22);
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
            this.toolStripLabel2.Size = new System.Drawing.Size(33, 22);
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
            this.findTypeComboBox.Size = new System.Drawing.Size(180, 23);
            this.findTypeComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.findTypeComboBox_KeyDown);
            // 
            // findComboBox
            // 
            this.findComboBox.AutoSize = false;
            this.findComboBox.Name = "findComboBox";
            this.findComboBox.Size = new System.Drawing.Size(200, 23);
            this.findComboBox.Leave += new System.EventHandler(this.findComboBox_Leave);
            this.findComboBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.findComboBox_KeyDown);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 25);
            // 
            // nextPacketBtn
            // 
            this.nextPacketBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.nextPacketBtn.Image = ((System.Drawing.Image)(resources.GetObject("nextPacketBtn.Image")));
            this.nextPacketBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.nextPacketBtn.Name = "nextPacketBtn";
            this.nextPacketBtn.Size = new System.Drawing.Size(26, 22);
            this.nextPacketBtn.Text = ">P";
            this.nextPacketBtn.ToolTipText = "Go to the next packet row";
            this.nextPacketBtn.Click += new System.EventHandler(this.nextPacketBtn_Click);
            // 
            // nextTransactionBtn
            // 
            this.nextTransactionBtn.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.nextTransactionBtn.Image = ((System.Drawing.Image)(resources.GetObject("nextTransactionBtn.Image")));
            this.nextTransactionBtn.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.nextTransactionBtn.Name = "nextTransactionBtn";
            this.nextTransactionBtn.Size = new System.Drawing.Size(26, 19);
            this.nextTransactionBtn.Text = ">T";
            this.nextTransactionBtn.ToolTipText = "Go to the next row belonging to a transaction";
            this.nextTransactionBtn.Click += new System.EventHandler(this.nextTransactionBtn_Click);
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
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.DataPropertyName = "Event";
            this.dataGridViewTextBoxColumn1.HeaderText = "Event";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.Visible = false;
            // 
            // eventTextCol
            // 
            this.eventTextCol.DataPropertyName = "Event";
            this.eventTextCol.HeaderText = "Event";
            this.eventTextCol.Name = "eventTextCol";
            this.eventTextCol.ReadOnly = true;
            this.eventTextCol.Visible = false;
            // 
            // indexTextCol
            // 
            this.indexTextCol.HeaderText = "Index";
            this.indexTextCol.Name = "indexTextCol";
            this.indexTextCol.ReadOnly = true;
            this.indexTextCol.Width = 58;
            // 
            // timestampTextCol
            // 
            this.timestampTextCol.HeaderText = "Timestamp";
            this.timestampTextCol.Name = "timestampTextCol";
            this.timestampTextCol.ReadOnly = true;
            this.timestampTextCol.Width = 83;
            // 
            // typeTextCol
            // 
            this.typeTextCol.HeaderText = "Type";
            this.typeTextCol.Name = "typeTextCol";
            this.typeTextCol.ReadOnly = true;
            this.typeTextCol.Width = 56;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(828, 585);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.mainSplitContainer);
            this.Controls.Add(this.menuStrip);
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.Text = "oSpy";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventsTbl)).EndInit();
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.dataGridContextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            this.dumpContextMenuStrip.ResumeLayout(false);
            this.propertyGridContextMenuStrip.ResumeLayout(false);
            this.mainSplitContainer.Panel1.ResumeLayout(false);
            this.mainSplitContainer.Panel2.ResumeLayout(false);
            this.mainSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.lowerSplitContainer.Panel1.ResumeLayout(false);
            this.lowerSplitContainer.Panel2.ResumeLayout(false);
            this.lowerSplitContainer.ResumeLayout(false);
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Data.DataSet dataSet;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearMenuItem;
        private System.Windows.Forms.BindingSource bindingSource;
        private System.Windows.Forms.ContextMenuStrip propertyGridContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem expandAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem collapseAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem autoHighlightMenuItem;
        private System.Windows.Forms.ContextMenuStrip dumpContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem saveRawDumpToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem analyzeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMSNP2PConversationsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem debugToolStripMenuItem1;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip dataGridContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem createSwRuleFromEntryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem manageSoftwallRulesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem aSCIIToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem hexToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem4;
        protected System.Windows.Forms.SplitContainer mainSplitContainer;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.SplitContainer lowerSplitContainer;
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
        private System.Windows.Forms.SaveFileDialog dumpSaveFileDialog;
        private System.Windows.Forms.ToolStripMenuItem goToReturnaddressInIDAToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem parserConfigToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showbacktraceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectallToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem rowsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem transactionsToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripButton nextPacketBtn;
        private System.Windows.Forms.ToolStripButton nextTransactionBtn;
        private System.Windows.Forms.ToolStripMenuItem goToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nextpacketToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem nextRowTransactionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewInternalDebugToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewWinCryptToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem applydebugSymbolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selBytesToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copyRawToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selSaveToFileToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem tryggveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newCaptureToolStripMenuItem;
        private System.Data.DataTable eventsTbl;
        private System.Data.DataColumn eventCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn eventTextCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn indexTextCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn timestampTextCol;
        private System.Windows.Forms.DataGridViewTextBoxColumn typeTextCol;
    }
}
