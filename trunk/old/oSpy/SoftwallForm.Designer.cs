namespace oSpy
{
    partial class SoftwallForm
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
            this.conditionsGroupBox = new System.Windows.Forms.GroupBox();
            this.remotePortTBox = new System.Windows.Forms.TextBox();
            this.softwallDataSet = new System.Data.DataSet();
            this.rulesTable = new System.Data.DataTable();
            this.nameCol = new System.Data.DataColumn();
            this.processNameCol = new System.Data.DataColumn();
            this.functionNameCol = new System.Data.DataColumn();
            this.retAddrCol = new System.Data.DataColumn();
            this.localAddrCol = new System.Data.DataColumn();
            this.localPortCol = new System.Data.DataColumn();
            this.remoteAddrCol = new System.Data.DataColumn();
            this.remotePortCol = new System.Data.DataColumn();
            this.retValCol = new System.Data.DataColumn();
            this.lastErrorCol = new System.Data.DataColumn();
            this.remoteAddrTBox = new System.Windows.Forms.TextBox();
            this.localPortTBox = new System.Windows.Forms.TextBox();
            this.localAddrTBox = new System.Windows.Forms.TextBox();
            this.remotePortCBox = new System.Windows.Forms.CheckBox();
            this.remoteAddrCBox = new System.Windows.Forms.CheckBox();
            this.localPortCBox = new System.Windows.Forms.CheckBox();
            this.localAddrCBox = new System.Windows.Forms.CheckBox();
            this.retAddrTBox = new System.Windows.Forms.TextBox();
            this.retAddrCBox = new System.Windows.Forms.CheckBox();
            this.funcNameTBox = new System.Windows.Forms.TextBox();
            this.funcNameCBox = new System.Windows.Forms.CheckBox();
            this.processNameTBox = new System.Windows.Forms.TextBox();
            this.processNameCBox = new System.Windows.Forms.CheckBox();
            this.actionsGroupBox = new System.Windows.Forms.GroupBox();
            this.lastErrorCBox = new System.Windows.Forms.ComboBox();
            this.retValCBox = new System.Windows.Forms.ComboBox();
            this.lastErrLbl = new System.Windows.Forms.Label();
            this.retValLbl = new System.Windows.Forms.Label();
            this.closeButton = new System.Windows.Forms.Button();
            this.ruleListView = new System.Windows.Forms.ListView();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.ruleListMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.newRuleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.conditionsGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.softwallDataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.rulesTable)).BeginInit();
            this.actionsGroupBox.SuspendLayout();
            this.ruleListMenuStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // conditionsGroupBox
            // 
            this.conditionsGroupBox.Controls.Add(this.remotePortTBox);
            this.conditionsGroupBox.Controls.Add(this.remoteAddrTBox);
            this.conditionsGroupBox.Controls.Add(this.localPortTBox);
            this.conditionsGroupBox.Controls.Add(this.localAddrTBox);
            this.conditionsGroupBox.Controls.Add(this.remotePortCBox);
            this.conditionsGroupBox.Controls.Add(this.remoteAddrCBox);
            this.conditionsGroupBox.Controls.Add(this.localPortCBox);
            this.conditionsGroupBox.Controls.Add(this.localAddrCBox);
            this.conditionsGroupBox.Controls.Add(this.retAddrTBox);
            this.conditionsGroupBox.Controls.Add(this.retAddrCBox);
            this.conditionsGroupBox.Controls.Add(this.funcNameTBox);
            this.conditionsGroupBox.Controls.Add(this.funcNameCBox);
            this.conditionsGroupBox.Controls.Add(this.processNameTBox);
            this.conditionsGroupBox.Controls.Add(this.processNameCBox);
            this.conditionsGroupBox.Enabled = false;
            this.conditionsGroupBox.Location = new System.Drawing.Point(139, 12);
            this.conditionsGroupBox.Name = "conditionsGroupBox";
            this.conditionsGroupBox.Size = new System.Drawing.Size(269, 190);
            this.conditionsGroupBox.TabIndex = 4;
            this.conditionsGroupBox.TabStop = false;
            this.conditionsGroupBox.Text = "Conditions";
            // 
            // remotePortTBox
            // 
            this.remotePortTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.RemotePort", true));
            this.remotePortTBox.Enabled = false;
            this.remotePortTBox.Location = new System.Drawing.Point(113, 160);
            this.remotePortTBox.Name = "remotePortTBox";
            this.remotePortTBox.Size = new System.Drawing.Size(75, 20);
            this.remotePortTBox.TabIndex = 17;
            // 
            // softwallDataSet
            // 
            this.softwallDataSet.DataSetName = "SoftwallDataSet";
            this.softwallDataSet.Tables.AddRange(new System.Data.DataTable[] {
            this.rulesTable});
            // 
            // rulesTable
            // 
            this.rulesTable.Columns.AddRange(new System.Data.DataColumn[] {
            this.nameCol,
            this.processNameCol,
            this.functionNameCol,
            this.retAddrCol,
            this.localAddrCol,
            this.localPortCol,
            this.remoteAddrCol,
            this.remotePortCol,
            this.retValCol,
            this.lastErrorCol});
            this.rulesTable.Constraints.AddRange(new System.Data.Constraint[] {
            new System.Data.UniqueConstraint("Constraint1", new string[] {
                        "Name"}, false)});
            this.rulesTable.TableName = "Rules";
            // 
            // nameCol
            // 
            this.nameCol.AllowDBNull = false;
            this.nameCol.ColumnName = "Name";
            // 
            // processNameCol
            // 
            this.processNameCol.ColumnName = "ProcessName";
            // 
            // functionNameCol
            // 
            this.functionNameCol.ColumnName = "FunctionName";
            // 
            // retAddrCol
            // 
            this.retAddrCol.ColumnName = "ReturnAddress";
            this.retAddrCol.DataType = typeof(uint);
            // 
            // localAddrCol
            // 
            this.localAddrCol.ColumnName = "LocalAddress";
            // 
            // localPortCol
            // 
            this.localPortCol.ColumnName = "LocalPort";
            this.localPortCol.DataType = typeof(ushort);
            // 
            // remoteAddrCol
            // 
            this.remoteAddrCol.ColumnName = "RemoteAddress";
            // 
            // remotePortCol
            // 
            this.remotePortCol.ColumnName = "RemotePort";
            this.remotePortCol.DataType = typeof(ushort);
            // 
            // retValCol
            // 
            this.retValCol.AllowDBNull = false;
            this.retValCol.ColumnName = "ReturnValue";
            this.retValCol.DataType = typeof(int);
            // 
            // lastErrorCol
            // 
            this.lastErrorCol.ColumnName = "LastError";
            this.lastErrorCol.DataType = typeof(uint);
            // 
            // remoteAddrTBox
            // 
            this.remoteAddrTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.RemoteAddress", true));
            this.remoteAddrTBox.Enabled = false;
            this.remoteAddrTBox.Location = new System.Drawing.Point(113, 137);
            this.remoteAddrTBox.Name = "remoteAddrTBox";
            this.remoteAddrTBox.Size = new System.Drawing.Size(150, 20);
            this.remoteAddrTBox.TabIndex = 16;
            // 
            // localPortTBox
            // 
            this.localPortTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.LocalPort", true));
            this.localPortTBox.Enabled = false;
            this.localPortTBox.Location = new System.Drawing.Point(113, 114);
            this.localPortTBox.Name = "localPortTBox";
            this.localPortTBox.Size = new System.Drawing.Size(75, 20);
            this.localPortTBox.TabIndex = 15;
            // 
            // localAddrTBox
            // 
            this.localAddrTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.LocalAddress", true));
            this.localAddrTBox.Enabled = false;
            this.localAddrTBox.Location = new System.Drawing.Point(113, 90);
            this.localAddrTBox.Name = "localAddrTBox";
            this.localAddrTBox.Size = new System.Drawing.Size(150, 20);
            this.localAddrTBox.TabIndex = 14;
            // 
            // remotePortCBox
            // 
            this.remotePortCBox.AutoSize = true;
            this.remotePortCBox.Location = new System.Drawing.Point(6, 162);
            this.remotePortCBox.Name = "remotePortCBox";
            this.remotePortCBox.Size = new System.Drawing.Size(87, 17);
            this.remotePortCBox.TabIndex = 13;
            this.remotePortCBox.Text = "Remote port:";
            this.remotePortCBox.UseVisualStyleBackColor = true;
            this.remotePortCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // remoteAddrCBox
            // 
            this.remoteAddrCBox.AutoSize = true;
            this.remoteAddrCBox.Location = new System.Drawing.Point(6, 139);
            this.remoteAddrCBox.Name = "remoteAddrCBox";
            this.remoteAddrCBox.Size = new System.Drawing.Size(106, 17);
            this.remoteAddrCBox.TabIndex = 12;
            this.remoteAddrCBox.Text = "Remote address:";
            this.remoteAddrCBox.UseVisualStyleBackColor = true;
            this.remoteAddrCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // localPortCBox
            // 
            this.localPortCBox.AutoSize = true;
            this.localPortCBox.Location = new System.Drawing.Point(6, 116);
            this.localPortCBox.Name = "localPortCBox";
            this.localPortCBox.Size = new System.Drawing.Size(76, 17);
            this.localPortCBox.TabIndex = 11;
            this.localPortCBox.Text = "Local port:";
            this.localPortCBox.UseVisualStyleBackColor = true;
            this.localPortCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // localAddrCBox
            // 
            this.localAddrCBox.AutoSize = true;
            this.localAddrCBox.Location = new System.Drawing.Point(6, 92);
            this.localAddrCBox.Name = "localAddrCBox";
            this.localAddrCBox.Size = new System.Drawing.Size(95, 17);
            this.localAddrCBox.TabIndex = 10;
            this.localAddrCBox.Text = "Local address:";
            this.localAddrCBox.UseVisualStyleBackColor = true;
            this.localAddrCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // retAddrTBox
            // 
            this.retAddrTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.ReturnAddress", true));
            this.retAddrTBox.Enabled = false;
            this.retAddrTBox.Location = new System.Drawing.Point(113, 65);
            this.retAddrTBox.Name = "retAddrTBox";
            this.retAddrTBox.Size = new System.Drawing.Size(75, 20);
            this.retAddrTBox.TabIndex = 9;
            // 
            // retAddrCBox
            // 
            this.retAddrCBox.AutoSize = true;
            this.retAddrCBox.Location = new System.Drawing.Point(6, 67);
            this.retAddrCBox.Name = "retAddrCBox";
            this.retAddrCBox.Size = new System.Drawing.Size(101, 17);
            this.retAddrCBox.TabIndex = 8;
            this.retAddrCBox.Text = "Return address:";
            this.retAddrCBox.UseVisualStyleBackColor = true;
            this.retAddrCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // funcNameTBox
            // 
            this.funcNameTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.FunctionName", true));
            this.funcNameTBox.Enabled = false;
            this.funcNameTBox.Location = new System.Drawing.Point(113, 42);
            this.funcNameTBox.Name = "funcNameTBox";
            this.funcNameTBox.Size = new System.Drawing.Size(150, 20);
            this.funcNameTBox.TabIndex = 7;
            // 
            // funcNameCBox
            // 
            this.funcNameCBox.AutoSize = true;
            this.funcNameCBox.Location = new System.Drawing.Point(6, 44);
            this.funcNameCBox.Name = "funcNameCBox";
            this.funcNameCBox.Size = new System.Drawing.Size(99, 17);
            this.funcNameCBox.TabIndex = 6;
            this.funcNameCBox.Text = "Function name:";
            this.funcNameCBox.UseVisualStyleBackColor = true;
            this.funcNameCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // processNameTBox
            // 
            this.processNameTBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.ProcessName", true));
            this.processNameTBox.Enabled = false;
            this.processNameTBox.Location = new System.Drawing.Point(113, 19);
            this.processNameTBox.Name = "processNameTBox";
            this.processNameTBox.Size = new System.Drawing.Size(150, 20);
            this.processNameTBox.TabIndex = 5;
            // 
            // processNameCBox
            // 
            this.processNameCBox.AutoSize = true;
            this.processNameCBox.Location = new System.Drawing.Point(6, 21);
            this.processNameCBox.Name = "processNameCBox";
            this.processNameCBox.Size = new System.Drawing.Size(96, 17);
            this.processNameCBox.TabIndex = 4;
            this.processNameCBox.Text = "Process name:";
            this.processNameCBox.UseVisualStyleBackColor = true;
            this.processNameCBox.CheckStateChanged += new System.EventHandler(this.conditionCBox_CheckStateChanged);
            // 
            // actionsGroupBox
            // 
            this.actionsGroupBox.Controls.Add(this.lastErrorCBox);
            this.actionsGroupBox.Controls.Add(this.retValCBox);
            this.actionsGroupBox.Controls.Add(this.lastErrLbl);
            this.actionsGroupBox.Controls.Add(this.retValLbl);
            this.actionsGroupBox.Enabled = false;
            this.actionsGroupBox.Location = new System.Drawing.Point(139, 208);
            this.actionsGroupBox.Name = "actionsGroupBox";
            this.actionsGroupBox.Size = new System.Drawing.Size(269, 68);
            this.actionsGroupBox.TabIndex = 5;
            this.actionsGroupBox.TabStop = false;
            this.actionsGroupBox.Text = "Actions if all conditions match:";
            // 
            // lastErrorCBox
            // 
            this.lastErrorCBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.LastError", true));
            this.lastErrorCBox.FormattingEnabled = true;
            this.lastErrorCBox.Items.AddRange(new object[] {
            "WSAEADDRINUSE (10048)",
            "WSAEADDRNOTAVAIL (10049)",
            "WSAENETDOWN (10050)",
            "WSAENETUNREACH (10051)",
            "WSAETIMEDOUT (10060)",
            "WSAECONNREFUSED (10061)",
            "WSAEHOSTUNREACH (10065)"});
            this.lastErrorCBox.Location = new System.Drawing.Point(113, 42);
            this.lastErrorCBox.Name = "lastErrorCBox";
            this.lastErrorCBox.Size = new System.Drawing.Size(150, 21);
            this.lastErrorCBox.TabIndex = 3;
            this.lastErrorCBox.Validating += new System.ComponentModel.CancelEventHandler(this.retValLastErrorCBox_Validating);
            // 
            // retValCBox
            // 
            this.retValCBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.softwallDataSet, "Rules.ReturnValue", true));
            this.retValCBox.FormattingEnabled = true;
            this.retValCBox.Items.AddRange(new object[] {
            "SOCKET_ERROR (-1)"});
            this.retValCBox.Location = new System.Drawing.Point(113, 18);
            this.retValCBox.Name = "retValCBox";
            this.retValCBox.Size = new System.Drawing.Size(150, 21);
            this.retValCBox.TabIndex = 2;
            this.retValCBox.Validating += new System.ComponentModel.CancelEventHandler(this.retValLastErrorCBox_Validating);
            // 
            // lastErrLbl
            // 
            this.lastErrLbl.AutoSize = true;
            this.lastErrLbl.Location = new System.Drawing.Point(6, 45);
            this.lastErrLbl.Name = "lastErrLbl";
            this.lastErrLbl.Size = new System.Drawing.Size(83, 13);
            this.lastErrLbl.TabIndex = 1;
            this.lastErrLbl.Text = "Set LastError to:";
            // 
            // retValLbl
            // 
            this.retValLbl.AutoSize = true;
            this.retValLbl.Location = new System.Drawing.Point(6, 21);
            this.retValLbl.Name = "retValLbl";
            this.retValLbl.Size = new System.Drawing.Size(71, 13);
            this.retValLbl.TabIndex = 0;
            this.retValLbl.Text = "Return value:";
            // 
            // closeButton
            // 
            this.closeButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.Location = new System.Drawing.Point(333, 282);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(75, 23);
            this.closeButton.TabIndex = 8;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            // 
            // ruleListView
            // 
            this.ruleListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.ruleListView.ContextMenuStrip = this.ruleListMenuStrip;
            this.ruleListView.FullRowSelect = true;
            this.ruleListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
            this.ruleListView.HideSelection = false;
            this.ruleListView.LabelEdit = true;
            this.ruleListView.Location = new System.Drawing.Point(12, 12);
            this.ruleListView.MultiSelect = false;
            this.ruleListView.Name = "ruleListView";
            this.ruleListView.Size = new System.Drawing.Size(121, 264);
            this.ruleListView.TabIndex = 9;
            this.ruleListView.UseCompatibleStateImageBehavior = false;
            this.ruleListView.View = System.Windows.Forms.View.List;
            this.ruleListView.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.rulesListView_ItemSelectionChanged);
            this.ruleListView.AfterLabelEdit += new System.Windows.Forms.LabelEditEventHandler(this.rulesListView_AfterLabelEdit);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Rules";
            this.columnHeader1.Width = 121;
            // 
            // ruleListMenuStrip
            // 
            this.ruleListMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newRuleToolStripMenuItem,
            this.deleteToolStripMenuItem});
            this.ruleListMenuStrip.Name = "ruleListMenuStrip";
            this.ruleListMenuStrip.Size = new System.Drawing.Size(128, 48);
            this.ruleListMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.ruleListMenuStrip_Opening);
            // 
            // newRuleToolStripMenuItem
            // 
            this.newRuleToolStripMenuItem.Name = "newRuleToolStripMenuItem";
            this.newRuleToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.newRuleToolStripMenuItem.Text = "&New rule";
            this.newRuleToolStripMenuItem.Click += new System.EventHandler(this.newRuleToolStripMenuItem_Click);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(127, 22);
            this.deleteToolStripMenuItem.Text = "&Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // SoftwallForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(420, 315);
            this.Controls.Add(this.ruleListView);
            this.Controls.Add(this.actionsGroupBox);
            this.Controls.Add(this.conditionsGroupBox);
            this.Controls.Add(this.closeButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "SoftwallForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manage softwall rules";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SoftwallForm_FormClosed);
            this.conditionsGroupBox.ResumeLayout(false);
            this.conditionsGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.softwallDataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.rulesTable)).EndInit();
            this.actionsGroupBox.ResumeLayout(false);
            this.actionsGroupBox.PerformLayout();
            this.ruleListMenuStrip.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox conditionsGroupBox;
        private System.Windows.Forms.TextBox retAddrTBox;
        private System.Windows.Forms.CheckBox retAddrCBox;
        private System.Windows.Forms.TextBox funcNameTBox;
        private System.Windows.Forms.CheckBox funcNameCBox;
        private System.Windows.Forms.TextBox processNameTBox;
        private System.Windows.Forms.CheckBox processNameCBox;
        private System.Windows.Forms.TextBox remotePortTBox;
        private System.Windows.Forms.TextBox remoteAddrTBox;
        private System.Windows.Forms.TextBox localPortTBox;
        private System.Windows.Forms.TextBox localAddrTBox;
        private System.Windows.Forms.CheckBox remotePortCBox;
        private System.Windows.Forms.CheckBox remoteAddrCBox;
        private System.Windows.Forms.CheckBox localPortCBox;
        private System.Windows.Forms.CheckBox localAddrCBox;
        private System.Windows.Forms.GroupBox actionsGroupBox;
        private System.Windows.Forms.ComboBox lastErrorCBox;
        private System.Windows.Forms.ComboBox retValCBox;
        private System.Windows.Forms.Label lastErrLbl;
        private System.Windows.Forms.Label retValLbl;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.ListView ruleListView;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Data.DataSet softwallDataSet;
        private System.Data.DataTable rulesTable;
        private System.Data.DataColumn processNameCol;
        private System.Data.DataColumn functionNameCol;
        private System.Data.DataColumn retAddrCol;
        private System.Data.DataColumn localAddrCol;
        private System.Data.DataColumn localPortCol;
        private System.Data.DataColumn remoteAddrCol;
        private System.Data.DataColumn remotePortCol;
        private System.Data.DataColumn retValCol;
        private System.Data.DataColumn lastErrorCol;
        private System.Data.DataColumn nameCol;
        private System.Windows.Forms.ContextMenuStrip ruleListMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem newRuleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;

    }
}