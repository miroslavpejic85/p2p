namespace p2pconn
{
    partial class pDesktop
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(pDesktop));
            this.p2pScreen = new System.Windows.Forms.PictureBox();
            this.panel1 = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.p2pScreen)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // p2pScreen
            // 
            this.p2pScreen.Location = new System.Drawing.Point(0, 0);
            this.p2pScreen.Name = "p2pScreen";
            this.p2pScreen.Size = new System.Drawing.Size(239, 167);
            this.p2pScreen.TabIndex = 0;
            this.p2pScreen.TabStop = false;
            this.p2pScreen.MouseDown += new System.Windows.Forms.MouseEventHandler(this.p2pScreen_MouseDown);
            this.p2pScreen.MouseMove += new System.Windows.Forms.MouseEventHandler(this.p2pScreen_MouseMove);
            this.p2pScreen.MouseUp += new System.Windows.Forms.MouseEventHandler(this.p2pScreen_MouseUp);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.SystemColors.Window;
            this.panel1.Controls.Add(this.p2pScreen);
            this.panel1.Location = new System.Drawing.Point(2, 1);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(533, 385);
            this.panel1.TabIndex = 27;
            // 
            // pDesktop
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(535, 387);
            this.Controls.Add(this.panel1);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.ForeColor = System.Drawing.Color.Red;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "pDesktop";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "P2P_Desktop";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.pDesktop_FormClosing);
            this.Load += new System.EventHandler(this.pDesktop_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.pDesktop_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.pDesktop_KeyUp);
            ((System.ComponentModel.ISupportInitialize)(this.p2pScreen)).EndInit();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.PictureBox p2pScreen;
        private System.Windows.Forms.Panel panel1;
    }
}