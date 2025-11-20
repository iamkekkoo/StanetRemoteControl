using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            connUscita = serv.ChiediAccesso(textBox1.Text, textBox2.Text, IdMacchina);
            if (connUscita.ID != 0) 
            {
                var b = true;

                textBox1.Size = new Size(0, 0);
                textBox2.Size = new Size(0, 0);
                button1.Size = new Size(0, 0);
                label1.Size = new Size(0, 0);

                this.BackColor = Color.Black;

                pictureBox1.Visible = true;

                connUscita.Screen = serv.GET(connUscita.Command, connUscita.ID);

                if (connUscita.Screen != null && connUscita.Screen != BitConverter.GetBytes(-1))
                {
                    if (this.Width >= this.Height)
                    {
                        ResizePictureBox((BytesToImage(connUscita.Screen).Width / BytesToImage(connUscita.Screen).Height) * pictureBox1.Height, this.Height);
                    }
                    else
                    {
                        ResizePictureBox(this.Width, (BytesToImage(connUscita.Screen).Height / BytesToImage(connUscita.Screen).Width) * pictureBox1.Width);
                    }
                    
                }
                else
                {
                    if (connUscita.Screen == BitConverter.GetBytes(-1)) { b = false; this.Dispose(); }//Connessione chiusa dal controllato
                }

                new Thread(async delegate ()
                {
                    converter = new ImageConverter();
                    while ( b ) 
                    {
                        connUscita.Screen = serv.GET(connUscita.Command, connUscita.ID);

                        if (connUscita.Screen != null && connUscita.Screen != BitConverter.GetBytes(-1))
                        {
                            if (this.Width >= this.Height)
                            {
                                await ResizePictureBox((BytesToImage(connUscita.Screen).Width / BytesToImage(connUscita.Screen).Height) * pictureBox1.Height, this.Height);
                            }
                            else
                            {
                                await ResizePictureBox(this.Width, (BytesToImage(connUscita.Screen).Height / BytesToImage(connUscita.Screen).Width) * pictureBox1.Width);
                            }
                            pictureBox1.Image = ResizeImage(BytesToImage(connUscita.Screen), pictureBox1.Width, pictureBox1.Height);
                        }
                        else 
                        {
                            if (connUscita.Screen == BitConverter.GetBytes(-1)) { b = false; this.Dispose(); }//Connessione chiusa dal controllato
                        }
                    }
                }).Start();

                if (connUscita.Screen == BitConverter.GetBytes(-1)) { this.Close(); this.Dispose(); }//Connessione chiusa dal controllato 
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

        private Task ResizePictureBox(int width, int height) 
        {
            pictureBox1.Width = width; pictureBox1.Height = height; return Task.CompletedTask;
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
