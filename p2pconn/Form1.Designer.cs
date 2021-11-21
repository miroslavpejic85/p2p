namespace p2pconn
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label3 = new System.Windows.Forms.Label();
            this.txtRemoteIP = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.txtnsg = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.btn_paste = new System.Windows.Forms.Button();
            this.r_chat = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.btnRdp = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.button3 = new System.Windows.Forms.Button();
            this.txtLocalHost = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.txtmyHost = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.dspeed = new System.Windows.Forms.ComboBox();
            this.lblFPS = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.lblkb = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.tabPage3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.label3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.Window;
            this.label3.Location = new System.Drawing.Point(5, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(86, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Peer IP:";
            // 
            // txtRemoteIP
            // 
            this.txtRemoteIP.BackColor = System.Drawing.SystemColors.Control;
            this.txtRemoteIP.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtRemoteIP.Location = new System.Drawing.Point(117, 63);
            this.txtRemoteIP.Name = "txtRemoteIP";
            this.txtRemoteIP.Size = new System.Drawing.Size(136, 14);
            this.txtRemoteIP.TabIndex = 9;
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.button2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.SystemColors.Window;
            this.button2.Location = new System.Drawing.Point(5, 88);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(329, 23);
            this.button2.TabIndex = 10;
            this.button2.Text = "Connect";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.SystemColors.Control;
            this.label4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.label4.Location = new System.Drawing.Point(5, 298);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(329, 13);
            this.label4.TabIndex = 11;
            this.label4.Text = "Idle...";
            // 
            // txtnsg
            // 
            this.txtnsg.BackColor = System.Drawing.SystemColors.Control;
            this.txtnsg.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtnsg.Location = new System.Drawing.Point(5, 234);
            this.txtnsg.Multiline = true;
            this.txtnsg.Name = "txtnsg";
            this.txtnsg.Size = new System.Drawing.Size(246, 57);
            this.txtnsg.TabIndex = 15;
            // 
            // button4
            // 
            this.button4.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.button4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button4.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button4.ForeColor = System.Drawing.SystemColors.Window;
            this.button4.Location = new System.Drawing.Point(254, 234);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(80, 27);
            this.button4.TabIndex = 16;
            this.button4.Text = "Send";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // btn_paste
            // 
            this.btn_paste.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.btn_paste.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_paste.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_paste.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btn_paste.ForeColor = System.Drawing.SystemColors.Window;
            this.btn_paste.Location = new System.Drawing.Point(278, 59);
            this.btn_paste.Name = "btn_paste";
            this.btn_paste.Size = new System.Drawing.Size(56, 23);
            this.btn_paste.TabIndex = 17;
            this.btn_paste.Text = "Paste";
            this.btn_paste.UseVisualStyleBackColor = false;
            this.btn_paste.Click += new System.EventHandler(this.btn_paste_Click);
            // 
            // r_chat
            // 
            this.r_chat.BackColor = System.Drawing.SystemColors.Control;
            this.r_chat.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.r_chat.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.r_chat.Location = new System.Drawing.Point(5, 117);
            this.r_chat.Name = "r_chat";
            this.r_chat.ReadOnly = true;
            this.r_chat.Size = new System.Drawing.Size(329, 111);
            this.r_chat.TabIndex = 18;
            this.r_chat.Text = "";
            this.r_chat.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.r_chat_LinkClicked);
            this.r_chat.TextChanged += new System.EventHandler(this.r_chat_TextChanged);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.SystemColors.Window;
            this.button1.Location = new System.Drawing.Point(278, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(56, 24);
            this.button1.TabIndex = 19;
            this.button1.Text = "copy";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.label5.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.SystemColors.Window;
            this.label5.Location = new System.Drawing.Point(5, 13);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 13);
            this.label5.TabIndex = 20;
            this.label5.Text = "My Wan IP:";
            // 
            // btnRdp
            // 
            this.btnRdp.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.btnRdp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRdp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRdp.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRdp.ForeColor = System.Drawing.SystemColors.Window;
            this.btnRdp.Location = new System.Drawing.Point(254, 264);
            this.btnRdp.Name = "btnRdp";
            this.btnRdp.Size = new System.Drawing.Size(80, 27);
            this.btnRdp.TabIndex = 21;
            this.btnRdp.Text = "Desktop";
            this.btnRdp.UseVisualStyleBackColor = false;
            this.btnRdp.Click += new System.EventHandler(this.btnRdp_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Location = new System.Drawing.Point(2, 1);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(348, 339);
            this.tabControl1.TabIndex = 22;
            // 
            // tabPage1
            // 
            this.tabPage1.BackColor = System.Drawing.Color.White;
            this.tabPage1.Controls.Add(this.button3);
            this.tabPage1.Controls.Add(this.txtLocalHost);
            this.tabPage1.Controls.Add(this.label13);
            this.tabPage1.Controls.Add(this.txtmyHost);
            this.tabPage1.Controls.Add(this.label5);
            this.tabPage1.Controls.Add(this.btnRdp);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Controls.Add(this.txtRemoteIP);
            this.tabPage1.Controls.Add(this.r_chat);
            this.tabPage1.Controls.Add(this.button2);
            this.tabPage1.Controls.Add(this.btn_paste);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.button4);
            this.tabPage1.Controls.Add(this.txtnsg);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(340, 313);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Peer";
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.button3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.ForeColor = System.Drawing.SystemColors.Window;
            this.button3.Location = new System.Drawing.Point(278, 32);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(56, 24);
            this.button3.TabIndex = 25;
            this.button3.Text = "copy";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // txtLocalHost
            // 
            this.txtLocalHost.BackColor = System.Drawing.SystemColors.Control;
            this.txtLocalHost.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtLocalHost.Location = new System.Drawing.Point(117, 39);
            this.txtLocalHost.Name = "txtLocalHost";
            this.txtLocalHost.ReadOnly = true;
            this.txtLocalHost.Size = new System.Drawing.Size(136, 14);
            this.txtLocalHost.TabIndex = 24;
            // 
            // label13
            // 
            this.label13.BackColor = System.Drawing.SystemColors.MenuHighlight;
            this.label13.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label13.ForeColor = System.Drawing.SystemColors.Window;
            this.label13.Location = new System.Drawing.Point(5, 38);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(86, 13);
            this.label13.TabIndex = 23;
            this.label13.Text = "My Lan IP:";
            // 
            // txtmyHost
            // 
            this.txtmyHost.BackColor = System.Drawing.SystemColors.Control;
            this.txtmyHost.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtmyHost.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtmyHost.Location = new System.Drawing.Point(117, 14);
            this.txtmyHost.Name = "txtmyHost";
            this.txtmyHost.ReadOnly = true;
            this.txtmyHost.Size = new System.Drawing.Size(136, 14);
            this.txtmyHost.TabIndex = 22;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.checkBox1);
            this.tabPage2.Controls.Add(this.dspeed);
            this.tabPage2.Controls.Add(this.lblFPS);
            this.tabPage2.Controls.Add(this.label15);
            this.tabPage2.Controls.Add(this.lblkb);
            this.tabPage2.Controls.Add(this.label14);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(340, 313);
            this.tabPage2.TabIndex = 2;
            this.tabPage2.Text = "Desktop";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.BackColor = System.Drawing.SystemColors.Window;
            this.checkBox1.Enabled = false;
            this.checkBox1.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkBox1.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.checkBox1.Location = new System.Drawing.Point(29, 21);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(133, 17);
            this.checkBox1.TabIndex = 7;
            this.checkBox1.Text = "Streach Desktop";
            this.checkBox1.UseVisualStyleBackColor = false;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // dspeed
            // 
            this.dspeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.dspeed.Enabled = false;
            this.dspeed.FormattingEnabled = true;
            this.dspeed.Items.AddRange(new object[] {
            "30",
            "50",
            "100",
            "200",
            "400",
            "800",
            "1000"});
            this.dspeed.Location = new System.Drawing.Point(70, 44);
            this.dspeed.Name = "dspeed";
            this.dspeed.Size = new System.Drawing.Size(58, 21);
            this.dspeed.TabIndex = 9;
            this.dspeed.SelectedIndexChanged += new System.EventHandler(this.dspeed_SelectedIndexChanged);
            // 
            // lblFPS
            // 
            this.lblFPS.AutoSize = true;
            this.lblFPS.BackColor = System.Drawing.Color.Transparent;
            this.lblFPS.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFPS.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.lblFPS.Location = new System.Drawing.Point(26, 98);
            this.lblFPS.Name = "lblFPS";
            this.lblFPS.Size = new System.Drawing.Size(37, 13);
            this.lblFPS.TabIndex = 4;
            this.lblFPS.Text = "FPS: ";
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.BackColor = System.Drawing.Color.Transparent;
            this.label15.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label15.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.label15.Location = new System.Drawing.Point(139, 47);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(19, 13);
            this.label15.TabIndex = 11;
            this.label15.Text = "ms";
            // 
            // lblkb
            // 
            this.lblkb.AutoSize = true;
            this.lblkb.BackColor = System.Drawing.Color.Transparent;
            this.lblkb.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblkb.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.lblkb.Location = new System.Drawing.Point(26, 85);
            this.lblkb.Name = "lblkb";
            this.lblkb.Size = new System.Drawing.Size(49, 13);
            this.lblkb.TabIndex = 5;
            this.lblkb.Text = "FSIZE: ";
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label14.ForeColor = System.Drawing.SystemColors.MenuHighlight;
            this.label14.Location = new System.Drawing.Point(26, 47);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(43, 13);
            this.label14.TabIndex = 10;
            this.label14.Text = "Speed:";
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.richTextBox1);
            this.tabPage3.Controls.Add(this.pictureBox1);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(340, 313);
            this.tabPage3.TabIndex = 1;
            this.tabPage3.Text = "About";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox1.Location = new System.Drawing.Point(29, 146);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(295, 121);
            this.richTextBox1.TabIndex = 1;
            this.richTextBox1.Text = "Remote Desktop P2P Open Source project!\n\nDeveloper: Miroslav Pejic\n\nGitHub: https" +
    "://github.com/miroslavpejic85\n\nLinkedin: https://www.linkedin.com/in/miroslav-pe" +
    "jic-976a07101/";
            this.richTextBox1.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.richTextBox1_LinkClicked);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::p2p.Properties.Resources.p2p1;
            this.pictureBox1.Location = new System.Drawing.Point(29, 32);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(96, 96);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(351, 343);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "P2P";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtRemoteIP;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtnsg;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button btn_paste;
        private System.Windows.Forms.RichTextBox r_chat;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnRdp;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txtmyHost;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Label lblFPS;
        private System.Windows.Forms.Label lblkb;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox txtLocalHost;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.ComboBox dspeed;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.RichTextBox richTextBox1;
    }
}

