using Microsoft.AspNet.SignalR.Client;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace StanetRemoteControl
{
    partial class Controllato
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            HubSchermo.Invoke("LeaveConnection", mac.ID);
            HubMouseMove.Invoke("LeaveConnection", mac.ID);
            HubPreciousCommand.Invoke("LeaveConnection", mac.ID);
            base.OnClosing(e);
        }

        #region Codice generato da Progettazione Windows Form

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InitializeComponent()
        {
            this.label5 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(9, 344);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(0, 18);
            this.label5.TabIndex = 5;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Monotype Corsiva", 36F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(74, 64);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(194, 57);
            this.label1.TabIndex = 0;
            this.label1.Text = "Il tuo ID";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(85, 123);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 31);
            this.label2.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.Color.Transparent;
            this.label4.Font = new System.Drawing.Font("Monotype Corsiva", 36F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(334, 64);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(196, 57);
            this.label4.TabIndex = 2;
            this.label4.Text = "Password";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(365, 123);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 31);
            this.label3.TabIndex = 3;
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 30F);
            this.button1.Location = new System.Drawing.Point(201, 227);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(197, 62);
            this.button1.TabIndex = 4;
            this.button1.Text = "Connetti";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Controllato
            // 
            this.AcceptButton = this.button1;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(600, 365);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.Name = "Controllato";
            this.Text = "Stanet Remote Control";
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        public string MioId
        {
            get { return label2.Text; }
            set { label2.Text = value; }
        }

        public string MiaPsw
        {
            get { return label3.Text; }
            set { label3.Text = value; }
        }

        public string Errore
        {
            get { return label5.Text; }
            set { label5.Text = value; }
        }

        internal static Macchina mac = new Macchina();
        public static List<Connessione> connIngresso = new List<Connessione>();
        public Controllore paginaConn = new Controllore();
        public static byte[] screenBytes;
        public static bool attivo = true;
        private HubConnection connection;
        private IHubProxy HubSchermo;
        private IHubProxy HubPreciousCommand;
        private IHubProxy HubMouseMove;
        private string _LatestMouseMoveCommandString = null;
        private readonly object _commandLock = new object();
        private System.Windows.Forms.Timer _renderTimer;
        private CancellationTokenSource cts;
        private Connessione connessione;
        private Bitmap _captureBitmap; // bitmap riutilizzabile per il monitor
        private Rectangle _monitorBounds; // salva le dimensioni del monitor
        private double ridimensionamento = 1;
        private static Screen[] screens = Screen.AllScreens;
        private Screen screen;
        private Rectangle bounds;
        private bool _sendingFrame = false;
        private static readonly ImageCodecInfo JpegEnc =
        Array.Find(
            ImageCodecInfo.GetImageEncoders(),
            e => e.MimeType == "image/jpeg"
        );

        private static readonly EncoderParameters Jpeg30 =
        new EncoderParameters(1)
        {
            Param = { [0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 30L) }
        };
        private Graphics _captureGraphics;

        public FileStream fs;
        public StreamReader reader;
        public StreamWriter writer;
        public string macchina;
        public int schermo = 0;

        static bool ctrlDown = false;
        static bool shiftDown = false;
        static bool altDown = false;

        public static Bitmap immagine = new Bitmap(Screen.AllScreens[0].Bounds.Width, Screen.AllScreens[0].Bounds.Height);

        public EventArgs COMANDO;

        public Label label1;
        public Label label2;
        public Label label3;
        public Label label4;
        public Label label5;
        public Button button1;
    }
}

