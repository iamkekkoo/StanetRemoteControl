using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using System.Windows.Input;

namespace StanetRemoteControl
{
    partial class Controllore
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
            connUscita.Command = "FINE";
            serv.GET(connUscita.Command, connUscita.ID);
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.Error = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBox1.Location = new System.Drawing.Point(80, 95);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(250, 53);
            this.textBox1.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBox2.Location = new System.Drawing.Point(464, 95);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(250, 53);
            this.textBox2.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(703, 405);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(85, 33);
            this.button1.TabIndex = 2;
            this.button1.Text = "Invio";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Error
            // 
            this.Error.AutoSize = true;
            this.Error.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.Error.ForeColor = System.Drawing.Color.Red;
            this.Error.Location = new System.Drawing.Point(12, 424);
            this.Error.Name = "Error";
            this.Error.Size = new System.Drawing.Size(0, 17);
            this.Error.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.Location = new System.Drawing.Point(80, 162);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(634, 208);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;

            /*pictureBox1.Click += (sender, e) => { connUscita.Command = JsonSerializer.Serialize<EventArgs>(e); };
            pictureBox1.DoubleClick += (sender, e) => { connUscita.Command = JsonSerializer.Serialize<EventArgs>(e); };
            pictureBox1.MouseClick += (sender, e) => { connUscita.Command = JsonSerializer.Serialize<System.Windows.Forms.MouseEventArgs>(new System.Windows.Forms.MouseEventArgs(e.Button, e.Clicks, pictureBox1.PointToClient(new System.Drawing.Point(e.X, e.Y)).X, pictureBox1.PointToClient(new System.Drawing.Point(e.X, e.Y)).Y, e.Delta)); };
            pictureBox1.MouseDoubleClick += (sender, e) => { connUscita.Command = JsonSerializer.Serialize<System.Windows.Forms.MouseEventArgs>(new System.Windows.Forms.MouseEventArgs(e.Button, e.Clicks, pictureBox1.PointToClient(new System.Drawing.Point(e.X, e.Y)).X, pictureBox1.PointToClient(new System.Drawing.Point(e.X, e.Y)).Y, e.Delta)); };
            pictureBox1.MouseMove += (sender, e) => { connUscita.Command = JsonSerializer.Serialize<System.Windows.Forms.MouseEventArgs>(new System.Windows.Forms.MouseEventArgs(e.Button, e.Clicks, pictureBox1.PointToClient(new System.Drawing.Point(e.X, e.Y)).X, pictureBox1.PointToClient(new System.Drawing.Point(e.X, e.Y)).Y, e.Delta)); };
            pictureBox1.PreviewKeyDown += (sender, e) => { connUscita.Command = JsonSerializer.Serialize<PreviewKeyDownEventArgs>(e); };
            */
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(19, 405);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 16);
            this.label1.TabIndex = 5;
            // 
            // Controllore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.Error);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Name = "Controllore";
            this.Text = "Stanet Remote Control";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        

        public IService1 serv = new RemoteControlNode.Service1Client();
        public Connessione connUscita = new Connessione();
        public Int64 IdMacchina;
        public ImageConverter converter;


        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label Error;
        private System.Windows.Forms.PictureBox pictureBox1;
        public System.Windows.Forms.Label label1;
    }
}