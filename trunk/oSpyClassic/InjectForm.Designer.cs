namespace oSpyClassic
{
    partial class InjectForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.asStartButton = new System.Windows.Forms.Button();
            this.asInjectButton = new System.Windows.Forms.Button();
            this.asKillButton = new System.Windows.Forms.Button();
            this.injectButton = new System.Windows.Forms.Button();
            this.refreshButton = new System.Windows.Forms.Button();
            this.killButton = new System.Windows.Forms.Button();
            this.closeButton = new System.Windows.Forms.Button();
            this.listView = new System.Windows.Forms.ListView();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Controls.Add(this.injectButton);
            this.panel1.Controls.Add(this.refreshButton);
            this.panel1.Controls.Add(this.killButton);
            this.panel1.Controls.Add(this.closeButton);
            this.panel1.Location = new System.Drawing.Point(277, 12);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(137, 265);
            this.panel1.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.asStartButton);
            this.groupBox1.Controls.Add(this.asInjectButton);
            this.groupBox1.Controls.Add(this.asKillButton);
            this.groupBox1.Location = new System.Drawing.Point(3, 113);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(129, 104);
            this.groupBox1.TabIndex = 7;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "ActiveSync processes";
            // 
            // asStartButton
            // 
            this.asStartButton.Location = new System.Drawing.Point(6, 46);
            this.asStartButton.Name = "asStartButton";
            this.asStartButton.Size = new System.Drawing.Size(117, 23);
            this.asStartButton.TabIndex = 10;
            this.asStartButton.Text = "Start";
            this.asStartButton.UseVisualStyleBackColor = true;
            this.asStartButton.Click += new System.EventHandler(this.asStartButton_Click);
            // 
            // asInjectButton
            // 
            this.asInjectButton.Location = new System.Drawing.Point(6, 75);
            this.asInjectButton.Name = "asInjectButton";
            this.asInjectButton.Size = new System.Drawing.Size(117, 23);
            this.asInjectButton.TabIndex = 9;
            this.asInjectButton.Text = "Inject";
            this.asInjectButton.UseVisualStyleBackColor = true;
            this.asInjectButton.Click += new System.EventHandler(this.asInjectButton_Click);
            // 
            // asKillButton
            // 
            this.asKillButton.Location = new System.Drawing.Point(6, 19);
            this.asKillButton.Name = "asKillButton";
            this.asKillButton.Size = new System.Drawing.Size(117, 23);
            this.asKillButton.TabIndex = 8;
            this.asKillButton.Text = "Kill";
            this.asKillButton.UseVisualStyleBackColor = true;
            this.asKillButton.Click += new System.EventHandler(this.asRestartButton_Click);
            // 
            // injectButton
            // 
            this.injectButton.Location = new System.Drawing.Point(3, 70);
            this.injectButton.Name = "injectButton";
            this.injectButton.Size = new System.Drawing.Size(123, 23);
            this.injectButton.TabIndex = 6;
            this.injectButton.Text = "&Inject";
            this.injectButton.UseVisualStyleBackColor = true;
            this.injectButton.Click += new System.EventHandler(this.injectButton_Click);
            // 
            // refreshButton
            // 
            this.refreshButton.Location = new System.Drawing.Point(3, 3);
            this.refreshButton.Name = "refreshButton";
            this.refreshButton.Size = new System.Drawing.Size(123, 23);
            this.refreshButton.TabIndex = 5;
            this.refreshButton.Text = "&Refresh";
            this.refreshButton.UseVisualStyleBackColor = true;
            this.refreshButton.Click += new System.EventHandler(this.refreshButton_Click);
            // 
            // killButton
            // 
            this.killButton.Location = new System.Drawing.Point(3, 41);
            this.killButton.Name = "killButton";
            this.killButton.Size = new System.Drawing.Size(123, 23);
            this.killButton.TabIndex = 4;
            this.killButton.Text = "&Kill";
            this.killButton.UseVisualStyleBackColor = true;
            this.killButton.Click += new System.EventHandler(this.killButton_Click);
            // 
            // closeButton
            // 
            this.closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.closeButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.closeButton.Location = new System.Drawing.Point(9, 239);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(119, 23);
            this.closeButton.TabIndex = 3;
            this.closeButton.Text = "&Close";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += new System.EventHandler(this.closeButton_Click);
            // 
            // listView
            // 
            this.listView.Location = new System.Drawing.Point(12, 12);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(259, 265);
            this.listView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this.listView.TabIndex = 4;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.List;
            // 
            // InjectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(415, 287);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "InjectForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Inject agent";
            this.Shown += new System.EventHandler(this.InjectForm_Shown);
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Button killButton;
        private System.Windows.Forms.Button refreshButton;
        private System.Windows.Forms.Button injectButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button asInjectButton;
        private System.Windows.Forms.Button asKillButton;
        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.Button asStartButton;

    }
}