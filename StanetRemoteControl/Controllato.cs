using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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
                    Graphics.FromImage(immagine).CopyFromScreen(new Point(rec.Left, rec.Top), Point.Empty, rec.Size);

                    var jpegCodec = ImageCodecInfo.GetImageEncoders().First(enc => enc.FormatID == ImageFormat.Jpeg.Guid);
                    var jpegParams = new EncoderParameters(1);
                    jpegParams.Param = new[] { new EncoderParameter(Encoder.Quality, 100L) };                    

                    // creat packet
                    //ImagePacket packet = new ImagePacket(ImageToByte(GetCompressedBitmap(immagine, 60L)));

                    connIngresso.Screen = GetCompressedBitmap(immagine, 60L);
                    connIngresso.Command = paginaConn.serv.POST(connIngresso.Screen, connIngresso.ID);

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

        public byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        private Image GetCompressedBitmap(Bitmap bmp, long quality)
        {
            using (var mss = new MemoryStream())
            {
                EncoderParameter qualityParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                ImageCodecInfo imageCodec = ImageCodecInfo.GetImageEncoders().FirstOrDefault(o => o.FormatID == ImageFormat.Jpeg.Guid);
                EncoderParameters parameters = new EncoderParameters(1);
                parameters.Param[0] = qualityParam;
                bmp.Save(mss, imageCodec, parameters);
                return Image.FromStream(mss);
            }
        }
    }
}
