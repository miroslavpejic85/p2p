using System.Drawing;
using System.Windows.Forms;

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
            label3 = new Label();
            txtRemoteIP = new TextBox();
            button2 = new Button();
            label4 = new Label();
            txtnsg = new TextBox();
            button4 = new Button();
            btn_paste = new Button();
            r_chat = new RichTextBox();
            button1 = new Button();
            label5 = new Label();
            btnRdp = new Button();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            button3 = new Button();
            txtLocalHost = new TextBox();
            label13 = new Label();
            txtmyHost = new TextBox();
            tabPage2 = new TabPage();
            button7 = new Button();
            button6 = new Button();
            dataGridView1 = new DataGridView();
            Server = new DataGridViewTextBoxColumn();
            Port = new DataGridViewTextBoxColumn();
            tabPage3 = new TabPage();
            checkBox1 = new CheckBox();
            dspeed = new ComboBox();
            lblFPS = new Label();
            label15 = new Label();
            lblkb = new Label();
            label14 = new Label();
            tabPage4 = new TabPage();
            richTextBox1 = new RichTextBox();
            pictureBox1 = new PictureBox();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            tabPage3.SuspendLayout();
            tabPage4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // label3
            // 
            label3.BackColor = SystemColors.MenuHighlight;
            label3.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.Window;
            label3.Location = new Point(5, 62);
            label3.Name = "label3";
            label3.Size = new Size(86, 13);
            label3.TabIndex = 8;
            label3.Text = "Peer IP:";
            // 
            // txtRemoteIP
            // 
            txtRemoteIP.BackColor = SystemColors.Control;
            txtRemoteIP.BorderStyle = BorderStyle.None;
            txtRemoteIP.Location = new Point(117, 63);
            txtRemoteIP.Name = "txtRemoteIP";
            txtRemoteIP.Size = new Size(136, 14);
            txtRemoteIP.TabIndex = 9;
            // 
            // button2
            // 
            button2.BackColor = SystemColors.MenuHighlight;
            button2.Cursor = Cursors.Hand;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button2.ForeColor = SystemColors.Window;
            button2.Location = new Point(5, 88);
            button2.Name = "button2";
            button2.Size = new Size(329, 23);
            button2.TabIndex = 10;
            button2.Text = "Connect";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // label4
            // 
            label4.BackColor = SystemColors.Control;
            label4.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.ForeColor = SystemColors.MenuHighlight;
            label4.Location = new Point(5, 298);
            label4.Name = "label4";
            label4.Size = new Size(329, 13);
            label4.TabIndex = 11;
            label4.Text = "Idle...";
            // 
            // txtnsg
            // 
            txtnsg.BackColor = SystemColors.Control;
            txtnsg.BorderStyle = BorderStyle.None;
            txtnsg.Location = new Point(5, 234);
            txtnsg.Multiline = true;
            txtnsg.Name = "txtnsg";
            txtnsg.Size = new Size(246, 57);
            txtnsg.TabIndex = 15;
            // 
            // button4
            // 
            button4.BackColor = SystemColors.MenuHighlight;
            button4.Cursor = Cursors.Hand;
            button4.FlatStyle = FlatStyle.Flat;
            button4.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button4.ForeColor = SystemColors.Window;
            button4.Location = new Point(254, 234);
            button4.Name = "button4";
            button4.Size = new Size(80, 27);
            button4.TabIndex = 16;
            button4.Text = "Send";
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // btn_paste
            // 
            btn_paste.BackColor = SystemColors.MenuHighlight;
            btn_paste.Cursor = Cursors.Hand;
            btn_paste.FlatStyle = FlatStyle.Flat;
            btn_paste.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btn_paste.ForeColor = SystemColors.Window;
            btn_paste.Location = new Point(278, 59);
            btn_paste.Name = "btn_paste";
            btn_paste.Size = new Size(56, 23);
            btn_paste.TabIndex = 17;
            btn_paste.Text = "Paste";
            btn_paste.UseVisualStyleBackColor = false;
            btn_paste.Click += btn_paste_Click;
            // 
            // r_chat
            // 
            r_chat.BackColor = SystemColors.Control;
            r_chat.BorderStyle = BorderStyle.None;
            r_chat.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            r_chat.Location = new Point(5, 117);
            r_chat.Name = "r_chat";
            r_chat.ReadOnly = true;
            r_chat.Size = new Size(329, 111);
            r_chat.TabIndex = 18;
            r_chat.Text = "";
            r_chat.LinkClicked += r_chat_LinkClicked;
            r_chat.TextChanged += r_chat_TextChanged;
            // 
            // button1
            // 
            button1.BackColor = SystemColors.MenuHighlight;
            button1.Cursor = Cursors.Hand;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button1.ForeColor = SystemColors.Window;
            button1.Location = new Point(278, 6);
            button1.Name = "button1";
            button1.Size = new Size(56, 24);
            button1.TabIndex = 19;
            button1.Text = "copy";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // label5
            // 
            label5.BackColor = SystemColors.MenuHighlight;
            label5.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = SystemColors.Window;
            label5.Location = new Point(5, 13);
            label5.Name = "label5";
            label5.Size = new Size(86, 13);
            label5.TabIndex = 20;
            label5.Text = "My Wan IP:";
            // 
            // btnRdp
            // 
            btnRdp.BackColor = SystemColors.MenuHighlight;
            btnRdp.Cursor = Cursors.Hand;
            btnRdp.FlatStyle = FlatStyle.Flat;
            btnRdp.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnRdp.ForeColor = SystemColors.Window;
            btnRdp.Location = new Point(254, 264);
            btnRdp.Name = "btnRdp";
            btnRdp.Size = new Size(80, 27);
            btnRdp.TabIndex = 21;
            btnRdp.Text = "Desktop";
            btnRdp.UseVisualStyleBackColor = false;
            btnRdp.Click += btnRdp_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new Point(2, 1);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(348, 339);
            tabControl1.TabIndex = 22;
            // 
            // tabPage1
            // 
            tabPage1.BackColor = Color.White;
            tabPage1.Controls.Add(button3);
            tabPage1.Controls.Add(txtLocalHost);
            tabPage1.Controls.Add(label13);
            tabPage1.Controls.Add(txtmyHost);
            tabPage1.Controls.Add(label5);
            tabPage1.Controls.Add(btnRdp);
            tabPage1.Controls.Add(label3);
            tabPage1.Controls.Add(button1);
            tabPage1.Controls.Add(txtRemoteIP);
            tabPage1.Controls.Add(r_chat);
            tabPage1.Controls.Add(button2);
            tabPage1.Controls.Add(btn_paste);
            tabPage1.Controls.Add(label4);
            tabPage1.Controls.Add(button4);
            tabPage1.Controls.Add(txtnsg);
            tabPage1.Location = new Point(4, 22);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(340, 313);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Peer";
            // 
            // button3
            // 
            button3.BackColor = SystemColors.MenuHighlight;
            button3.Cursor = Cursors.Hand;
            button3.FlatStyle = FlatStyle.Flat;
            button3.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button3.ForeColor = SystemColors.Window;
            button3.Location = new Point(278, 32);
            button3.Name = "button3";
            button3.Size = new Size(56, 24);
            button3.TabIndex = 25;
            button3.Text = "copy";
            button3.UseVisualStyleBackColor = false;
            button3.Click += button3_Click;
            // 
            // txtLocalHost
            // 
            txtLocalHost.BackColor = SystemColors.Control;
            txtLocalHost.BorderStyle = BorderStyle.None;
            txtLocalHost.Location = new Point(117, 39);
            txtLocalHost.Name = "txtLocalHost";
            txtLocalHost.ReadOnly = true;
            txtLocalHost.Size = new Size(136, 14);
            txtLocalHost.TabIndex = 24;
            // 
            // label13
            // 
            label13.BackColor = SystemColors.MenuHighlight;
            label13.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label13.ForeColor = SystemColors.Window;
            label13.Location = new Point(5, 38);
            label13.Name = "label13";
            label13.Size = new Size(86, 13);
            label13.TabIndex = 23;
            label13.Text = "My Lan IP:";
            // 
            // txtmyHost
            // 
            txtmyHost.BackColor = SystemColors.Control;
            txtmyHost.BorderStyle = BorderStyle.None;
            txtmyHost.Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtmyHost.Location = new Point(117, 14);
            txtmyHost.Name = "txtmyHost";
            txtmyHost.ReadOnly = true;
            txtmyHost.Size = new Size(136, 14);
            txtmyHost.TabIndex = 22;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(button7);
            tabPage2.Controls.Add(button6);
            tabPage2.Controls.Add(dataGridView1);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Size = new Size(340, 311);
            tabPage2.TabIndex = 3;
            tabPage2.Text = "Stun";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // button7
            // 
            button7.BackColor = SystemColors.MenuHighlight;
            button7.Cursor = Cursors.Hand;
            button7.FlatStyle = FlatStyle.Flat;
            button7.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button7.ForeColor = SystemColors.Window;
            button7.Location = new Point(6, 9);
            button7.Name = "button7";
            button7.Size = new Size(65, 24);
            button7.TabIndex = 22;
            button7.Text = "Save";
            button7.UseVisualStyleBackColor = false;
            button7.Click += button7_Click;
            // 
            // button6
            // 
            button6.BackColor = SystemColors.MenuHighlight;
            button6.Cursor = Cursors.Hand;
            button6.FlatStyle = FlatStyle.Flat;
            button6.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            button6.ForeColor = SystemColors.Window;
            button6.Location = new Point(77, 9);
            button6.Name = "button6";
            button6.Size = new Size(65, 24);
            button6.TabIndex = 21;
            button6.Text = "Delete";
            button6.UseVisualStyleBackColor = false;
            button6.Click += button6_Click;
            // 
            // dataGridView1
            // 
            dataGridView1.BackgroundColor = SystemColors.Control;
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { Server, Port });
            dataGridView1.Location = new Point(6, 39);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.Size = new Size(327, 269);
            dataGridView1.TabIndex = 0;
            // 
            // Server
            // 
            Server.HeaderText = "Server";
            Server.Name = "Server";
            Server.Width = 170;
            // 
            // Port
            // 
            Port.HeaderText = "Port";
            Port.Name = "Port";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(checkBox1);
            tabPage3.Controls.Add(dspeed);
            tabPage3.Controls.Add(lblFPS);
            tabPage3.Controls.Add(label15);
            tabPage3.Controls.Add(lblkb);
            tabPage3.Controls.Add(label14);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(340, 311);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Desktop";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.BackColor = SystemColors.Window;
            checkBox1.Enabled = false;
            checkBox1.Font = new Font("Verdana", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            checkBox1.ForeColor = SystemColors.MenuHighlight;
            checkBox1.Location = new Point(29, 18);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(133, 17);
            checkBox1.TabIndex = 7;
            checkBox1.Text = "Streach Desktop";
            checkBox1.UseVisualStyleBackColor = false;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // dspeed
            // 
            dspeed.DropDownStyle = ComboBoxStyle.DropDownList;
            dspeed.Enabled = false;
            dspeed.FormattingEnabled = true;
            dspeed.Items.AddRange(new object[] { "30", "50", "100", "200", "400", "800", "1000" });
            dspeed.Location = new Point(70, 48);
            dspeed.Name = "dspeed";
            dspeed.Size = new Size(58, 21);
            dspeed.TabIndex = 9;
            dspeed.SelectedIndexChanged += dspeed_SelectedIndexChanged;
            // 
            // lblFPS
            // 
            lblFPS.AutoSize = true;
            lblFPS.BackColor = Color.Transparent;
            lblFPS.Font = new Font("Consolas", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblFPS.ForeColor = SystemColors.MenuHighlight;
            lblFPS.Location = new Point(26, 98);
            lblFPS.Name = "lblFPS";
            lblFPS.Size = new Size(37, 13);
            lblFPS.TabIndex = 4;
            lblFPS.Text = "FPS: ";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.BackColor = Color.Transparent;
            label15.Font = new Font("Consolas", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label15.ForeColor = SystemColors.MenuHighlight;
            label15.Location = new Point(134, 52);
            label15.Name = "label15";
            label15.Size = new Size(19, 13);
            label15.TabIndex = 11;
            label15.Text = "MS";
            // 
            // lblkb
            // 
            lblkb.AutoSize = true;
            lblkb.BackColor = Color.Transparent;
            lblkb.Font = new Font("Consolas", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblkb.ForeColor = SystemColors.MenuHighlight;
            lblkb.Location = new Point(26, 85);
            lblkb.Name = "lblkb";
            lblkb.Size = new Size(49, 13);
            lblkb.TabIndex = 5;
            lblkb.Text = "FSIZE: ";
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Font = new Font("Consolas", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label14.ForeColor = SystemColors.MenuHighlight;
            label14.Location = new Point(26, 52);
            label14.Name = "label14";
            label14.Size = new Size(43, 13);
            label14.TabIndex = 10;
            label14.Text = "DELAY:";
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(richTextBox1);
            tabPage4.Controls.Add(pictureBox1);
            tabPage4.Location = new Point(4, 22);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(340, 313);
            tabPage4.TabIndex = 1;
            tabPage4.Text = "About";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Location = new Point(29, 146);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(295, 92);
            richTextBox1.TabIndex = 1;
            richTextBox1.Text = "Remote Desktop P2P based.\n\nGitHub: https://github.com/miroslavpejic85/p2p\n\nLinkedin: https://www.linkedin.com/in/miroslav-pejic-976a07101/";
            richTextBox1.LinkClicked += richTextBox1_LinkClicked;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.p2p;
            pictureBox1.Location = new Point(29, 24);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(96, 96);
            pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 13F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(351, 343);
            Controls.Add(tabControl1);
            Font = new Font("Verdana", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "P2P";
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            tabPage4.ResumeLayout(false);
            tabPage4.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
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
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.TextBox txtmyHost;
        private System.Windows.Forms.TabPage tabPage3;
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
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.DataGridViewTextBoxColumn Server;
        private System.Windows.Forms.DataGridViewTextBoxColumn Port;
    }
}

