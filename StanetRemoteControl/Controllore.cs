using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NPOI.HSSF.Util.HSSFColor;
using static NPOI.POIFS.Crypt.CryptoFunctions;


namespace StanetRemoteControl
{
    public partial class Controllore : Form
    {
        public Controllore()
        {
            InitializeComponent();
            this.pictureBox1.Visible = false;
            this.Load += Controllore_Load;

            return;
        }
        private void Controllore_Load(object sender, EventArgs e) 
        {
            cts = new CancellationTokenSource();
            return;
        }

        private async Task InizializzaSignalR()
        {
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
            Console.WriteLine("connessione riuscita");
            StartRenderTimer();
            StartSendMouseMoveCommandTimer();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            connUscita = serv.ChiediAccesso(textBox1.Text, textBox2.Text, IdMacchina);
            
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
            _renderTimer.Interval = 16; // ~30 FPS
            _renderTimer.Tick += RenderFrame;
            _renderTimer.Start();
        }

        private void StartSendMouseMoveCommandTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 100; // ~15 FPS
            _renderTimer.Tick += SendMouseMoveCommand;
            _renderTimer.Start();
        }

        private void RenderFrame(object sender, EventArgs e)
        {
            byte[] frame;

            lock (_frameLock)
            {
                if (_latestFrame == null)
                    return;

                frame = _latestFrame;
            }

            using (var bmp = (Bitmap)BytesToImage(frame))
            {
                pictureBox1.Image?.Dispose();
                pictureBox1.Image = (Bitmap)BytesToImage(_latestFrame);
                Console.WriteLine("picturebox aggiornata");
            }
        }
        
        private void SendMouseMoveCommand(object sender, EventArgs e)
        {
            if (_LastMouseMoveCommandToSend == null)
                return;

            HubPreciousCommand.Invoke("SendCommand", connUscita.ID_Controllato, _LastMouseMoveCommandToSend);
            Console.WriteLine("MouseMove Inviato");
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

        public static Image BytesToImage(byte[] value)
        {
            Image clone;
            try
            {
                using (var ms = new MemoryStream(value))
                {
                    Image img = Image.FromStream(ms);

                    // Clona l’immagine per staccarla dal MemoryStream
                    clone = new Bitmap(img);

                    img.Dispose(); // smaltisci l’originale

                   
                }
            }
            catch (Exception e)
            { 
                // Se i byte non sono validi, ritorna un'immagine nera 800x600
                clone = new Bitmap(800, 600);
                using (Graphics g = Graphics.FromImage(clone))
                {
                    g.Clear(Color.Black);
                }
            }
            return clone;
        }
         
    }
}
