namespace oSpy.Capture
{
    partial class ProgressForm
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.msgBytesLabel = new System.Windows.Forms.Label();
            this.msgCountLabel = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.pktBytesLabel = new System.Windows.Forms.Label();
            this.pktCountLabel = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.stopButton = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.msgBytesLabel);
            this.groupBox1.Controls.Add(this.msgCountLabel);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(268, 80);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Messages:";
            // 
            // msgBytesLabel
            // 
            this.msgBytesLabel.Location = new System.Drawing.Point(67, 43);
            this.msgBytesLabel.Name = "msgBytesLabel";
            this.msgBytesLabel.Size = new System.Drawing.Size(180, 23);
            this.msgBytesLabel.TabIndex = 6;
            this.msgBytesLabel.Text = "0";
            this.msgBytesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // msgCountLabel
            // 
            this.msgCountLabel.Location = new System.Drawing.Point(67, 21);
            this.msgCountLabel.Name = "msgCountLabel";
            this.msgCountLabel.Size = new System.Drawing.Size(180, 23);
            this.msgCountLabel.TabIndex = 5;
            this.msgCountLabel.Text = "0";
            this.msgCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(17, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(36, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Bytes:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(17, 26);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 3;
            this.label3.Text = "Count:";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.pktBytesLabel);
            this.groupBox2.Controls.Add(this.pktCountLabel);
            this.groupBox2.Controls.Add(this.label7);
            this.groupBox2.Controls.Add(this.label8);
            this.groupBox2.Location = new System.Drawing.Point(12, 98);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(268, 79);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Packets:";
            // 
            // pktBytesLabel
            // 
            this.pktBytesLabel.Location = new System.Drawing.Point(64, 43);
            this.pktBytesLabel.Name = "pktBytesLabel";
            this.pktBytesLabel.Size = new System.Drawing.Size(183, 23);
            this.pktBytesLabel.TabIndex = 6;
            this.pktBytesLabel.Text = "0";
            this.pktBytesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pktCountLabel
            // 
            this.pktCountLabel.Location = new System.Drawing.Point(61, 21);
            this.pktCountLabel.Name = "pktCountLabel";
            this.pktCountLabel.Size = new System.Drawing.Size(186, 23);
            this.pktCountLabel.TabIndex = 5;
            this.pktCountLabel.Text = "0";
            this.pktCountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(17, 48);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(36, 13);
            this.label7.TabIndex = 4;
            this.label7.Text = "Bytes:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(17, 26);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(38, 13);
            this.label8.TabIndex = 3;
            this.label8.Text = "Count:";
            // 
            // stopButton
            // 
            this.stopButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.stopButton.Location = new System.Drawing.Point(113, 183);
            this.stopButton.Name = "stopButton";
            this.stopButton.Size = new System.Drawing.Size(75, 22);
            this.stopButton.TabIndex = 5;
            this.stopButton.Text = "&Stop";
            this.stopButton.UseVisualStyleBackColor = true;
            this.stopButton.Click += new System.EventHandler(this.stopButton_Click);
            // 
            // ProgressForm
            // 
            this.AcceptButton = this.stopButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.stopButton;
            this.ClientSize = new System.Drawing.Size(292, 217);
            this.Controls.Add(this.stopButton);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Name = "ProgressForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Capture";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label msgBytesLabel;
        private System.Windows.Forms.Label msgCountLabel;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label pktBytesLabel;
        private System.Windows.Forms.Label pktCountLabel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Button stopButton;
    }
}