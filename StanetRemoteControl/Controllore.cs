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
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            await connection.Start();
            await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
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
            _renderTimer.Interval = 4; // ~240 FPS
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

        private void RenderFrame(object sender, EventArgs e)
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
            InitializeComponent();
            this.pictureBox1.Visible = false;
            this.Load += Controllore_Load;
            this.Shown += Controllore_Shown;
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
