using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using Newtonsoft.Json;

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
                        if (connUscita.Screen != null) {
                            if ((int)connUscita.Screen.CompareTo("FINE") == 0) { b = false; this.Dispose(); }//Connessione chiusa dal controllato
                            pictureBox1.Image = ResizeImage((Bitmap)Newtonsoft.Json.JsonConvert.DeserializeObject(connUscita.Screen), pictureBox1.Width, pictureBox1.Height);
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
    }

    public class immagine {
        public object Tag;
        public struct PhysicalDimension 
        {
            public bool IsEmpty;
            public int Width;
            public int Height;
        }
        public struct Size 
        {
            public bool IsEmpty;
            public int Width;
            public int Height;
        }
        public int Width;
        public int Height;
        public int HorizontalResolution;
        public int VerticalResolution;
        public int Flags;
        public struct RawFormat 
        {
            public string Guid;
        }
        public int PixelFormat;
        public struct Palette 
        {
            public int Flags;
            public List<Object> Entries;
        }
        public List<String> FrameDimensionsList;
        public List<object> PropertyIdList;
        public List<object> PropertyItems;
    }

}
