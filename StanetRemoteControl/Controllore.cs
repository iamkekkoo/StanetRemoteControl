using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace StanetRemoteControl
{
    public partial class Controllore : Form
    {
        public Controllore()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            connUscita = serv.ChiediAccesso(textBox1.Text, textBox2.Text, IdMacchina);
            if (connUscita.ID != 0) 
            {
                textBox1.Visible = false;
                textBox2.Visible = false;
                button1.Visible = false;
                label1.Visible = false;

                this.Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                this.Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;

                pictureBox1.Width = this.Width;
                pictureBox1.Height = this.Height;

                pictureBox1.Visible = true;
                new Thread(delegate ()
                {
                    converter = new ImageConverter();
                    var b = true;
                    while ( b ) 
                    {
                        connUscita.Screen = serv.GET(connUscita.Command, connUscita.ID);

                        //ImagePacket packet = System.Text.Json.JsonSerializer.Deserialize<ImagePacket>(connUscita.Screen);

                        //byte[] receive_bytes = packet.GetRawData();
                        // conver bytes to Image
                        //Image receive_image = BytesToImage(packet.GetRawData());

                        if (connUscita.Screen != null && connUscita.Screen != BitConverter.GetBytes(-1))
                        {
                            pictureBox1.Image = ResizeImage(BytesToImage(connUscita.Screen), pictureBox1.Width, pictureBox1.Height);
                        }
                        else 
                        {
                            if (connUscita.Screen == BitConverter.GetBytes(-1)) { b = false; this.Dispose(); }//Connessione chiusa dal controllato
                        }
                    }
                }).Start();
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
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

        public static Image BytesToImage(byte[] value)
        {
            using (var ms = new MemoryStream(value))
            {
                return Image.FromStream(ms);
            }
        }
    }
}
