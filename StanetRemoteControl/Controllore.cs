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


            // Ricezione screen
            hub.On<byte[]>("ReceiveScreen", screenBytes => {
                pictureBox1.Image.Dispose();
                pictureBox1.Image = ResizeImage((Bitmap)BytesToImage(screenBytes), pictureBox1.Width, pictureBox1.Height);
                Console.WriteLine("picturebox aggiornata");
            });  

            // riconnessione semplice a SignalR
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
