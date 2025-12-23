using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static NPOI.HSSF.Util.HSSFColor;


namespace StanetRemoteControl
{
    public partial class Controllore : Form
    {
        public Controllore()
        {
            InitializeComponent();
            
            this.pictureBox1.Visible = false;
        }

        private async Task InizializzaSignalR()
        {
            connection = new HubConnection("https://francescostdev.azurewebsites.net");
            hub = connection.CreateHubProxy("ScreenHub");
            pictureBox1.BackColor = Color.Black;
            // Ricezione screen
            hub.On<byte[]>("ReceiveScreen", screenBytes =>
            {
                // UI thread
                pictureBox1.BeginInvoke(new Action(() =>
                {
                    pictureBox1.Image?.Dispose();
                    pictureBox1.Image = ResizeImage((Bitmap)BytesToImage(screenBytes), pictureBox1.Width, pictureBox1.Height);
                }));
            });

            // riconnessione semplice
            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await hub.Invoke("JoinConnection", connUscita.ID);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            await connection.Start();
            await hub.Invoke("JoinConnection", connUscita.ID);
            Console.WriteLine("connessione riuscita");
        }


        private void button1_Click(object sender, EventArgs e)
        {

            connUscita = serv.ChiediAccesso(textBox1.Text, textBox2.Text, IdMacchina);
            
            if (connUscita.ID != 0) 
            {
                Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;

                pictureBox1.Visible = true;

                //_ = AggiornaPictureBoxAsync();
                _ = InizializzaSignalR();

            }
        }

        public async Task AggiornaPictureBoxAsync() 
        {
            bool attivo = true;
            while (attivo)
            {
                try
                {
                    connUscita.Screen = await serv.GETAsync(connUscita.Command, connUscita.ID);
                    await Task.Delay(5);
                    if (connUscita.Screen != null && connUscita.Screen != await Task.Run(() => BitConverter.GetBytes(1)))
                     {
                        immagine = await Task.Run(() => (Bitmap)BytesToImage(connUscita.Screen));
                        await Task.Delay(5);
                        /*resize dell'immagine rispetto alla grandezza della finestra (lasciando le proporzioni dell'immagine)*/
                        if (pictureBox1.Width >= pictureBox1.Height)
                        { pictureBox1.Image = await Task.Run(() => (Image)(new Bitmap(immagine, immagine.Width * pictureBox1.Height / immagine.Height, pictureBox1.Height))); await Task.Delay(5); }
                        else
                        { pictureBox1.Image = await Task.Run(() => (Image)(new Bitmap(immagine, pictureBox1.Width, immagine.Height * pictureBox1.Width / immagine.Width))); await Task.Delay(5); }

                    }
                    else
                    {
                        if (connUscita.Screen == BitConverter.GetBytes(1)) { attivo = false; this.Dispose(); } //Connessione chiusa dal controllato
                    }
                }
                catch (Exception E) { }
                await Task.Delay(5);
            }
        }

        // ====== UTILS ======

        private static Bitmap ResizeImage(Bitmap src, int maxW, int maxH)
        {
            float ratio = Math.Min(
                (float)maxW / src.Width,
                (float)maxH / src.Height);

            int w = (int)(src.Width * ratio);
            int h = (int)(src.Height * ratio);

            var bmp = new Bitmap(w, h);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(src, 0, 0, w, h);
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
