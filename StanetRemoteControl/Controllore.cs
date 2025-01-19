using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Interop;

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
                textBox1.Size = new Size(0, 0);
                textBox2.Size = new Size(0, 0);
                button1.Size = new Size(0, 0);
                label1.Size = new Size(0, 0);

                this.Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                this.Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;

                pictureBox1.Width = this.Width;
                pictureBox1.Height = this.Height;
                pictureBox1.SetBounds(0,0,this.Width,this.Height);
                pictureBox1.BackColor = Color.Black;
                pictureBox1.Visible = true;
                new Thread(delegate ()
                {
                    attivo = true;
                    while ( attivo && connUscita.Screen != BitConverter.GetBytes(0)) 
                    {
                        connUscita.Screen = serv.GET(connUscita.Command, connUscita.ID);
                        if (connUscita.Screen != null)
                        {
                            /*resize dell'immagine rispetto alla grandezza della finestra (lasciando le proporzioni dell'immagine)*/
                            if (pictureBox1.Width >= pictureBox1.Height)
                            { pictureBox1.Image = (Image)(new Bitmap(BytesToImage(connUscita.Screen), pictureBox1.Width, BytesToImage(connUscita.Screen).Height * pictureBox1.Width / BytesToImage(connUscita.Screen).Width)); }
                            else
                            { pictureBox1.Image = (Image)(new Bitmap(BytesToImage(connUscita.Screen), BytesToImage(connUscita.Screen).Width * pictureBox1.Height / BytesToImage(connUscita.Screen).Height, pictureBox1.Height)); }
                        }
                        else 
                        {
                            if (connUscita.Screen == BitConverter.GetBytes(0)) { attivo = false; this.Dispose(); }//Connessione chiusa dal controllato
                        }
                    }
                }).Start();
            }
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
