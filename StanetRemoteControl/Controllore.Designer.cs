using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            this.KeyPreview = true;
            this.KeyDown += Controllore_KeyDown;
            this.KeyUp += Controllore_KeyUp;

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
            this.textBox1.Location = new System.Drawing.Point(60, 77);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(189, 44);
            this.textBox1.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBox2.Location = new System.Drawing.Point(348, 77);
            this.textBox2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(189, 44);
            this.textBox2.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(528, 329);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 27);
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
            this.Error.Location = new System.Drawing.Point(9, 344);
            this.Error.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Error.Name = "Error";
            this.Error.Size = new System.Drawing.Size(0, 13);
            this.Error.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            
            pictureBox1.Dock = DockStyle.Fill; // opzionale, fa riempire tutta la finestra
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseUp += PictureBox1_MouseUp;

            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.ForeColor = System.Drawing.Color.Red;
            this.label1.Location = new System.Drawing.Point(14, 329);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(0, 13);
            this.label1.TabIndex = 5;
            // 
            // Controllore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 365);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.Error);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "Controllore";
            this.Text = "Stanet Remote Control";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            

            int x = e.X * 1000 / pictureBox1.Width;
            int y = e.Y * 1000 / pictureBox1.Height;

            var cmd = new MouseCommand
            {
                Type = "MOUSE",
                X = x,
                Y = y,
                Action = "MOVE"
            };

            SendMouseCommand(cmd);
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            string action = e.Button == MouseButtons.Left ? "LEFT_DOWN" :
                            e.Button == MouseButtons.Right ? "RIGHT_DOWN" : "MIDDLE_DOWN";

            var cmd = new MouseCommand
            {
                Type = "MOUSE",
                X = e.X * 1000 / pictureBox1.Width,
                Y = e.Y * 1000 / pictureBox1.Height,
                Action = action
            };

            SendMouseCommand(cmd);
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            string action = e.Button == MouseButtons.Left ? "LEFT_UP" :
                            e.Button == MouseButtons.Right ? "RIGHT_UP" : "MIDDLE_UP";

            var cmd = new MouseCommand
            {
                Type = "MOUSE",
                X = e.X * 1000 / pictureBox1.Width,
                Y = e.Y * 1000 / pictureBox1.Height,
                Action = action
            };

            SendMouseCommand(cmd);
        }

        private void Controllore_KeyDown(object sender, KeyEventArgs e)
        {
            var cmd = new KeyCommand
            {
                Type = "KEY",
                Key = e.KeyCode.ToString(),
                Action = "DOWN"
            };
            SendKeyCommand(cmd);
        }

        private void Controllore_KeyUp(object sender, KeyEventArgs e)
        {
            var cmd = new KeyCommand
            {
                Type = "KEY",
                Key = e.KeyCode.ToString(),
                Action = "UP"
            };
            SendKeyCommand(cmd);
        }



        private void SendMouseCommand(MouseCommand cmd)
        {
            if (hub == null || connection == null || connection.State != ConnectionState.Connected)
            { return; }

            try
            {
                hub.Invoke("SendCommand", connUscita.ID, JsonConvert.SerializeObject(cmd));
                Task.Delay(100);
            }
            catch { }
        }
        private void SendKeyCommand(KeyCommand cmd)
        {
            if (hub == null || connection == null || connection.State != ConnectionState.Connected)
            { return; }

            try
            {
                hub.Invoke("SendCommand", connUscita.ID, JsonConvert.SerializeObject(cmd));
                Task.Delay(100);
            }
            catch { }
        }
        #endregion


        public static IService1 serv = new RemoteControlNode.Service1Client();
        private Connessione connUscita = new Connessione();
        public Int64 IdMacchina;

        public static Bitmap immagine = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);
        private HubConnection connection;
        private IHubProxy hub;

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label Error;
        private System.Windows.Forms.Label label1;
        private PictureBox pictureBox1;
    }
}