using Microsoft.AspNet.SignalR.Client;
using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
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

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            this.DoubleBuffered = true; // evita sfarfallio

            DrawGradientBackground(e.Graphics, this.ClientRectangle);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            HubSchermo.Invoke("LeaveConnection", connUscita.ID_Controllato);
            HubMouseMove.Invoke("LeaveConnection", connUscita.ID_Controllato);
            HubPreciousCommand.Invoke("LeaveConnection", connUscita.ID_Controllato);
            base.OnClosing(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Rubrica?.Close();
            base.OnFormClosing(e);
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
            this.rubrica = new System.Windows.Forms.Button();
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
            this.button1.Click += (s,e) => button1_Click(textBox1.Text, textBox2.Text);
            // 
            // rubrica
            // 
            this.rubrica.Location = new System.Drawing.Point(this.Top, this.Right);
            this.rubrica.Name = "rubrica";
            this.rubrica.TabIndex = 2;
            this.rubrica.Text = "Rubrica";
            this.rubrica.UseVisualStyleBackColor = true;
            this.rubrica.Click += new System.EventHandler(this.btnToggleRubrica_Click);
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
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;

            pictureBox1.Paint += (s, e) =>
            {
                e.Graphics.InterpolationMode = InterpolationMode.Low;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                e.Graphics.CompositingQuality = CompositingQuality.HighSpeed;
            };

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
            //this.BackColor = Color.FromArgb(255, 30, 87, 153); // rgba(30,87,153,1)
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.rubrica);
            this.Controls.Add(this.Error);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "Controllore";
            this.Text = "Stanet Remote Control";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.LocationChanged += Controllore_LocationChanged;
            this.SizeChanged += Controllore_SizeChanged;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void Controllore_LocationChanged(object sender, EventArgs e)
        {
            if (Rubrica != null)
                Rubrica.SyncPosition();
        }

        private void Controllore_SizeChanged(object sender, EventArgs e)
        {
            if (Rubrica != null)
                Rubrica.SyncPosition();
        }

        private void btnToggleRubrica_Click(object sender, EventArgs e)
        {
            Rubrica.animTimer.Start();
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (TryGetImageCoordinates(pictureBox1, e.Location, out var coordinates)) //calcolo e mando il comando del mouse solo se il mouse è nella picturebox
            {
                var cmd = new MouseCommand
                {
                    Type = "MOUSE",
                    X = coordinates.X,
                    Y = coordinates.Y,
                    Action = "MOVE"
                };

                SendMouseCommand(cmd);
            }
        }

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            string action = e.Button == MouseButtons.Left ? "LEFT_DOWN" :
                            e.Button == MouseButtons.Right ? "RIGHT_DOWN" : "MIDDLE_DOWN";

            if (TryGetImageCoordinates(pictureBox1, e.Location, out var coordinates)) //calcolo e mando il comando del mouse solo se il mouse è nella picturebox
            {
                var cmd = new MouseCommand
                {
                    Type = "MOUSE",
                    X = coordinates.X,
                    Y = coordinates.Y,
                    Action = action
                };

                SendMouseCommand(cmd);
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            string action = e.Button == MouseButtons.Left ? "LEFT_UP" :
                            e.Button == MouseButtons.Right ? "RIGHT_UP" : "MIDDLE_UP";

            if (TryGetImageCoordinates(pictureBox1, e.Location, out var coordinates)) //calcolo e mando il comando del mouse solo se il mouse è nella picturebox
            {
                var cmd = new MouseCommand
                {
                    Type = "MOUSE",
                    X = coordinates.X,
                    Y = coordinates.Y,
                    Action = action
                };

                SendMouseCommand(cmd);
            }
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
            if (HubMouseMove == null ||  HubPreciousCommand == null || connection == null || connection.State != ConnectionState.Connected)
            { return; }

            try
            {
                if (cmd.Action.CompareTo("MOVE") == 0 ) { _LastMouseMoveCommandToSend = JsonConvert.SerializeObject(cmd); }
                else { HubPreciousCommand.Invoke("SendCommand", connUscita.ID_Controllato, JsonConvert.SerializeObject(cmd)); }
            }
            catch { }
        }
      
        private void SendKeyCommand(KeyCommand cmd)
        {
            if (HubPreciousCommand == null || connection == null || connection.State != ConnectionState.Connected)
            { return; }

            try
            {
                HubPreciousCommand.Invoke("SendCommand", connUscita.ID_Controllato, JsonConvert.SerializeObject(cmd));
            }
            catch { }
        }

        public static bool TryGetImageCoordinates(PictureBox pictureBox, Point mouseLocation, out Point imagePoint)
        {
            imagePoint = Point.Empty;

            if (pictureBox.Image == null)
                return false;

            Size imgSize = pictureBox.Image.Size;
            Size pbSize = pictureBox.ClientSize;

            float ratioWidth = (float)pbSize.Width / imgSize.Width;
            float ratioHeight = (float)pbSize.Height / imgSize.Height;
            float scale = Math.Min(ratioWidth, ratioHeight);

            int displayedWidth = (int)(imgSize.Width * scale);
            int displayedHeight = (int)(imgSize.Height * scale);

            int offsetX = (pbSize.Width - displayedWidth) / 2;
            int offsetY = (pbSize.Height - displayedHeight) / 2;

            if (mouseLocation.X < offsetX || mouseLocation.X > offsetX + displayedWidth ||
                mouseLocation.Y < offsetY || mouseLocation.Y > offsetY + displayedHeight)
                return false;

            int x = (int)((mouseLocation.X - offsetX) / scale);
            int y = (int)((mouseLocation.Y - offsetY) / scale);

            imagePoint = new Point(x, y);
            return true;
        }

        private void DrawGradientBackground(Graphics g, Rectangle rect)
        {
            float angle = 135f;

            Color[] colors = new Color[]
            {
                Color.FromArgb(0, 30, 87, 153),    // 0%
                Color.FromArgb(204, 30, 87, 153),  // 15%
                Color.FromArgb(255, 30, 87, 153),  // 19%
                Color.FromArgb(255, 30, 87, 153),  // 20%
                Color.FromArgb(255, 41, 137, 216), // 50%
                Color.FromArgb(255, 30, 87, 153),  // 80%
                Color.FromArgb(255, 30, 87, 153),  // 81%
                Color.FromArgb(204, 30, 87, 153),  // 85%
                Color.FromArgb(0, 30, 87, 153)     // 100%
            };

            float[] positions = new float[] { 0f, 0.15f, 0.19f, 0.20f, 0.50f, 0.80f, 0.81f, 0.85f, 1f };

            using (LinearGradientBrush brush = new LinearGradientBrush(rect, Color.White, Color.White, angle))
            {
                ColorBlend blend = new ColorBlend();
                blend.Colors = colors;
                blend.Positions = positions;
                brush.InterpolationColors = blend;

                g.FillRectangle(brush, rect);
            }
        }

        #endregion


        public static IService1 serv = new RemoteControlNode.Service1Client();
        internal Connessione connUscita = new Connessione();
        internal Int64 IdMacchina;
        internal Int64? IdSottoscrizione;
        byte[] _latestFrame = null;
        internal byte[] _Frame = null;
        private readonly object _frameLock = new object();
        private System.Windows.Forms.Timer _renderTimer;
        public static Bitmap immagine = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);
        internal HubConnection connection;
        internal IHubProxy HubSchermo;
        internal IHubProxy HubPreciousCommand;
        internal IHubProxy HubMouseMove;
        private CancellationTokenSource cts = new CancellationTokenSource();//
        private string _LastMouseMoveCommandToSend = null;
        private string _LastMouseMoveCommandSent = null;
        private Bitmap _displayBitmap;
        private readonly object _bitmapLock = new object();
        private Rubrica Rubrica;
        private Bitmap bmp;

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label Error;
        private System.Windows.Forms.Label label1;
        private PictureBox pictureBox1;
        private System.Windows.Forms.Button rubrica;
    }
}