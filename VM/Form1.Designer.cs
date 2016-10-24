namespace VM
{
    partial class Form1
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.speedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.realTimeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.mSToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.Diagnostic = new System.Windows.Forms.Panel();
            this.lblRegister = new System.Windows.Forms.Label();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.vmScreen1 = new VM.VMScreen();
            this.mSToolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.mSToolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.mSToolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.Diagnostic.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(801, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.speedToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.openToolStripMenuItem.Text = "&Open...";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // speedToolStripMenuItem
            // 
            this.speedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mSToolStripMenuItem,
            this.realTimeToolStripMenuItem,
            this.mSToolStripMenuItem1,
            this.mSToolStripMenuItem2,
            this.mSToolStripMenuItem3,
            this.mSToolStripMenuItem4});
            this.speedToolStripMenuItem.Name = "speedToolStripMenuItem";
            this.speedToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.speedToolStripMenuItem.Text = "&Speed";
            // 
            // mSToolStripMenuItem
            // 
            this.mSToolStripMenuItem.Name = "mSToolStripMenuItem";
            this.mSToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.mSToolStripMenuItem.Text = "500MS";
            this.mSToolStripMenuItem.Click += new System.EventHandler(this.mSToolStripMenuItem_Click);
            // 
            // realTimeToolStripMenuItem
            // 
            this.realTimeToolStripMenuItem.Name = "realTimeToolStripMenuItem";
            this.realTimeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.realTimeToolStripMenuItem.Text = "Real Time";
            this.realTimeToolStripMenuItem.Click += new System.EventHandler(this.realTimeToolStripMenuItem_Click);
            // 
            // mSToolStripMenuItem1
            // 
            this.mSToolStripMenuItem1.Name = "mSToolStripMenuItem1";
            this.mSToolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
            this.mSToolStripMenuItem1.Text = "250MS";
            this.mSToolStripMenuItem1.Click += new System.EventHandler(this.mSToolStripMenuItem1_Click);
            // 
            // Diagnostic
            // 
            this.Diagnostic.Controls.Add(this.lblRegister);
            this.Diagnostic.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.Diagnostic.Location = new System.Drawing.Point(0, 548);
            this.Diagnostic.Name = "Diagnostic";
            this.Diagnostic.Size = new System.Drawing.Size(801, 54);
            this.Diagnostic.TabIndex = 2;
            // 
            // lblRegister
            // 
            this.lblRegister.AccessibleName = "lblRegister";
            this.lblRegister.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRegister.Font = new System.Drawing.Font("Courier New", 10F);
            this.lblRegister.Location = new System.Drawing.Point(0, 0);
            this.lblRegister.Name = "lblRegister";
            this.lblRegister.Size = new System.Drawing.Size(801, 54);
            this.lblRegister.TabIndex = 0;
            this.lblRegister.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.DefaultExt = "VM";
            this.openFileDialog1.Filter = "|*.VM";
            // 
            // vmScreen1
            // 
            this.vmScreen1.BackColor = System.Drawing.Color.Black;
            this.vmScreen1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vmScreen1.Location = new System.Drawing.Point(0, 24);
            this.vmScreen1.Name = "vmScreen1";
            this.vmScreen1.ScreenMemoryLocation = ((ushort)(0));
            this.vmScreen1.Size = new System.Drawing.Size(801, 578);
            this.vmScreen1.TabIndex = 0;
            // 
            // mSToolStripMenuItem2
            // 
            this.mSToolStripMenuItem2.Name = "mSToolStripMenuItem2";
            this.mSToolStripMenuItem2.Size = new System.Drawing.Size(152, 22);
            this.mSToolStripMenuItem2.Text = "1000MS";
            this.mSToolStripMenuItem2.Click += new System.EventHandler(this.mSToolStripMenuItem2_Click);
            // 
            // mSToolStripMenuItem3
            // 
            this.mSToolStripMenuItem3.Name = "mSToolStripMenuItem3";
            this.mSToolStripMenuItem3.Size = new System.Drawing.Size(152, 22);
            this.mSToolStripMenuItem3.Text = "2000MS";
            this.mSToolStripMenuItem3.Click += new System.EventHandler(this.mSToolStripMenuItem3_Click);
            // 
            // mSToolStripMenuItem4
            // 
            this.mSToolStripMenuItem4.Name = "mSToolStripMenuItem4";
            this.mSToolStripMenuItem4.Size = new System.Drawing.Size(152, 22);
            this.mSToolStripMenuItem4.Text = "3000MS";
            this.mSToolStripMenuItem4.Click += new System.EventHandler(this.mSToolStripMenuItem4_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(801, 602);
            this.Controls.Add(this.Diagnostic);
            this.Controls.Add(this.vmScreen1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "Form1";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.Diagnostic.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private VMScreen vmScreen1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.Panel Diagnostic;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label lblRegister;
        private System.Windows.Forms.ToolStripMenuItem speedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem realTimeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem mSToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem mSToolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem mSToolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem mSToolStripMenuItem4;
    }
}

