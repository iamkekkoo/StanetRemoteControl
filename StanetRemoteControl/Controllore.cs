using Microsoft.AspNet.SignalR.Client;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace StanetRemoteControl
{
    public partial class Controllore : Form
    {
        public Controllore()
        {
            InitializeComponent();
            this.pictureBox1.Visible = false;
            this.Load += Controllore_Load;
            this.Shown += Controllore_Shown;

            return;
        }
        private void Controllore_Load(object sender, EventArgs e) 
        {
            cts = new CancellationTokenSource();
            return;
        }

        private void Controllore_Shown(object sender, EventArgs e)
        {
            Rubrica = new Rubrica(this, serv, IdMacchina);
        }

        private async Task InizializzaSignalR()
        {
            connection = new HubConnection("https://francescostdev.azurewebsites.net");
            HubSchermo = connection.CreateHubProxy("ScreenHub");
            HubPreciousCommand = connection.CreateHubProxy("PreciousCommandHub");
            HubMouseMove = connection.CreateHubProxy("MouseMoveHub");

            // Ricezione screen
            HubSchermo.On<byte[]>("ReceiveScreen", screenBytes => {
                lock (_frameLock)
                {
                    _latestFrame = screenBytes;
                }            
            });

            // riconnessione semplice a SignalR
            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            await connection.Start();
            await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
            Console.WriteLine("Schermo connesso a: " + connUscita.ID_Controllato);
            await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
            await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
            StartRenderTimer();
            StartSendMouseMoveCommandTimer();
        }

        public void button1_Click(string UserName, string Password)
        {

            connUscita = serv.ChiediAccesso(UserName, Password, IdMacchina);
            
            if (connUscita.ID != 0) 
            {
                textBox1.Size = new Size(0, 0);
                textBox2.Size = new Size(0, 0);
                button1.Size = new Size(0, 0);

                Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;
                
                //proviamo cosi
                pictureBox1.Width = Width; pictureBox1.Height = Height;

                pictureBox1.Visible = true;

                //_ = AggiornaPictureBoxAsync();
                _ = InizializzaSignalR();

            }
        }

        private void StartRenderTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 3; // ~240 FPS   //provo da 4->3 (240 ->360 fps)
            _renderTimer.Tick += RenderFrame;
            _renderTimer.Start();
        }

        private void StartSendMouseMoveCommandTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 100; // ~10 FPS
            _renderTimer.Tick += SendMouseMoveCommand;
            _renderTimer.Start();
        }

        private unsafe void RenderFrame(object sender, EventArgs e)
        {
            if (_latestFrame == null) return;
            if (_latestFrame.Length < 8) return;

            int frameWidth = BitConverter.ToInt32(_latestFrame, 0);
            int frameHeight = BitConverter.ToInt32(_latestFrame, 4);

            Bitmap tempBmp;
            using (var ms = new MemoryStream(_latestFrame, 8, _latestFrame.Length - 8))
                tempBmp = new Bitmap(ms);

            // Crea bitmap riutilizzabile
            if (_displayBitmap == null || _displayBitmap.Width != frameWidth || _displayBitmap.Height != frameHeight)
            {
                lock (_bitmapLock)
                {
                    _displayBitmap?.Dispose();
                    _displayBitmap = new Bitmap(frameWidth, frameHeight, PixelFormat.Format24bppRgb);
                    pictureBox1.Image = _displayBitmap;
                }
            }

            // LockBits + unsafe copy
            lock (_bitmapLock)
            {
                Rectangle rect = new Rectangle(0, 0, frameWidth, frameHeight);

                BitmapData destData = _displayBitmap.LockBits(rect, ImageLockMode.WriteOnly, _displayBitmap.PixelFormat);
                BitmapData srcData = tempBmp.LockBits(rect, ImageLockMode.ReadOnly, tempBmp.PixelFormat);

                int bytes = Math.Abs(destData.Stride) * frameHeight;

                unsafe
                {
                    byte* srcPtr = (byte*)srcData.Scan0;
                    byte* destPtr = (byte*)destData.Scan0;
                    Buffer.MemoryCopy(srcPtr, destPtr, bytes, bytes);
                }

                _displayBitmap.UnlockBits(destData);
                tempBmp.UnlockBits(srcData);
            }

            tempBmp.Dispose();

            // Aggiorna UI
            pictureBox1.BeginInvoke(new MethodInvoker(() => pictureBox1.Invalidate()));
        }

        private void SendMouseMoveCommand(object sender, EventArgs e)
        {
            if (_LastMouseMoveCommandToSend == null)
                return;

            if (_LastMouseMoveCommandToSend != _LastMouseMoveCommandSent) 
            { 
                HubPreciousCommand.Invoke("SendCommand", connUscita.ID_Controllato, _LastMouseMoveCommandToSend);
                _LastMouseMoveCommandSent = _LastMouseMoveCommandToSend;
            }
            
        }

        public void ControlloreReset() 
        {
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
            this.pictureBox1.Visible = false;
            // 
            // Controllore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 365);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        }

        // ====== UTILS ======

        private static Bitmap ResizeImage(Bitmap src, int maxW, int maxH)
        {
            var bmp = new Bitmap(maxW, maxH);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, maxW, maxH);
            }

            src.Dispose();
            return bmp;
        }

        public static System.Drawing.Image BytesToImage(byte[] value)
        {
            try
            {
                using (var ms = new MemoryStream(value))
                using (var img = System.Drawing.Image.FromStream(ms, false, false))
                {
                    return (Bitmap)new Bitmap(img).Clone();
                }
            }
            catch (Exception e)
            {
                // Se i byte non sono validi, ritorna un'immagine nera 800x600
                Bitmap bmp = new Bitmap(800, 600, PixelFormat.Format24bppRgb);
                using (Graphics g = Graphics.FromImage(bmp))
                    g.Clear(Color.Black);
                return bmp;
            }
        }
        

    }
}
