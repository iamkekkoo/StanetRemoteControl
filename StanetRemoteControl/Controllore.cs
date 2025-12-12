using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;

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

                _ = AggiornaPictureBoxAsync();

            }
        }

        public async Task AggiornaPictureBoxAsync() 
        {
            attivo = true;
            while (attivo)
            {
                try
                {
                    connUscita.Screen = await serv.GETAsync(connUscita.Command, connUscita.ID);
                    if (connUscita.Screen != null && connUscita.Screen != await Task.Run(() => BitConverter.GetBytes(1)))
                     {
                        immagine = await Task.Run(() => (Bitmap)BytesToImage(connUscita.Screen));
                        /*resize dell'immagine rispetto alla grandezza della finestra (lasciando le proporzioni dell'immagine)*/
                        if (pictureBox1.Width >= pictureBox1.Height)
                        { pictureBox1.Image = await Task.Run(() => (Image)(new Bitmap(immagine, immagine.Width * pictureBox1.Height / immagine.Height, pictureBox1.Height))); attivo = true; }
                        else
                        { pictureBox1.Image = await Task.Run(() => (Image)(new Bitmap(immagine, pictureBox1.Width, immagine.Height * pictureBox1.Width / immagine.Width))); attivo = true; }

                    }
                    else
                    {
                        if (connUscita.Screen == BitConverter.GetBytes(1)) { attivo = false; this.Dispose(); } //Connessione chiusa dal controllato
                    }
                }
                catch (Exception E) { }
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
