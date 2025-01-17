using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using StanetRemoteControl.RemoteControlNode;

namespace StanetRemoteControl
{
    public partial class Controllato : Form
    {
        public Controllato(string macchina)
        {
            InitializeComponent();
            immagine = new Bitmap(rec.Width, rec.Height);

            if (macchina.CompareTo("") == 0)
            {
                //file appena creato -> vuoto
                //Se non presente la registra

                mac = paginaConn.serv.RegistraMacchina(new Macchina());
                
                //File.WriteAllText(filename, JsonSerializer.Serialize<Macchina>(pagina.mac));
            }
            else
            {
                /*altrimenti utilizza la macchina registrata*/
                mac = System.Text.Json.JsonSerializer.Deserialize<Macchina>(macchina);
            }

            connIngresso = paginaConn.serv.ApriConnessione(mac);

            /*Se la connessione è disponibile parte il thread di condivisione dello schermo (e lettura dei comandi)*/
            new Thread(delegate() 
            {
                converter = new ImageConverter();
                var b = true;
                while ( b ) 
                {
                    Graphics.FromImage(immagine).CopyFromScreen(0, 0, 0, 0, rec.Size);
                    connIngresso.Screen = imageToByteArray(immagine);
                    connIngresso.Command = paginaConn.serv.POST(connIngresso.Screen, connIngresso.ID);
                    //pictureBox1.Image = ResizeImage(immagine, pictureBox1.Width+1, pictureBox1.Height+1);
                    /*esegue operazione a seconda del comando*/
                    switch (connIngresso.Command) 
                    {
                        case "FINE":
                            /*termina la comunicazione col client*/
                            b = false;
                            
                            break;

                        default:
                            try
                            {
                                if (connIngresso.Command != null)
                                { COMANDO = System.Text.Json.JsonSerializer.Deserialize<EventArgs>(connIngresso.Command); }
                            }
                            catch (Exception e) { }
                            break;
                    }

                }
            }).Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(delegate ()
            {
                paginaConn = new Controllore();
                Application.EnableVisualStyles();
                paginaConn.IdMacchina = mac.ID;
                Application.Run(paginaConn);
            }).Start();
        }

        public byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        public static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
