using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace StanetRemoteControl
{
    public partial class Controllore : Form
    {
        public Controllore()
        {
            InitializeComponent();
            this.pictureBox1.Visible = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            connUscita = serv.ChiediAccesso(textBox1.Text, textBox2.Text, IdMacchina);
            serv.GET("3000", connUscita.ID);
            if (connUscita.ID != 0) 
            {
                textBox1.Size = new Size(0, 0);
                textBox2.Size = new Size(0, 0);
                button1.Size = new Size(0, 0);
                label1.Size = new Size(0, 0);

                this.Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                this.Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;

                pictureBox1.Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100 - 5;
                pictureBox1.Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100 - 50;
                pictureBox1.SetBounds(0,0, Screen.PrimaryScreen.Bounds.Width * 60 / 100 - 5, Screen.PrimaryScreen.Bounds.Height * 60 / 100 - 50);
                pictureBox1.BackColor = Color.Black;
                pictureBox1.Visible = true;

                    attivo = true;
                    while (attivo)
                    {
                        connUscita.Screen = serv.GET(connUscita.Command, connUscita.ID);
                        if (connUscita.Screen != null && connUscita.Screen != BitConverter.GetBytes(1) && connUscita.Screen != BitConverter.GetBytes(100))
                        {
                            /*resize dell'immagine rispetto alla grandezza della finestra (lasciando le proporzioni dell'immagine)*/
                            if (pictureBox1.Width >= pictureBox1.Height)
                            { pictureBox1.Image = (Image)(new Bitmap(BytesToImage(connUscita.Screen), BytesToImage(connUscita.Screen).Width * pictureBox1.Height / BytesToImage(connUscita.Screen).Height, pictureBox1.Height)); }
                            else
                            { pictureBox1.Image = (Image)(new Bitmap(BytesToImage(connUscita.Screen), pictureBox1.Width, BytesToImage(connUscita.Screen).Height * pictureBox1.Width / BytesToImage(connUscita.Screen).Width)); }

                            }
                        else
                        {
                            if (connUscita.Screen == BitConverter.GetBytes(100)) { attivo = false; this.Dispose(); } //Connessione chiusa dal controllato
                        }
                        
                    }
                    
                
            }
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
