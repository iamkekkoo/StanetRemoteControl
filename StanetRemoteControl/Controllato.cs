using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using StanetRemoteControl.RemoteControlNode;

namespace StanetRemoteControl
{
    public partial class Controllato : Form
    {
        public Controllato()
        {
            InitializeComponent();

            immagine = new Bitmap(rec.Width, rec.Height);

            /*Se la connessione è disponibile parte il thread di condivisione dello schermo (e lettura dei comandi)*/
            new Thread(delegate() 
            {
                while (connIngresso.Command.CompareTo("FINE") != 0) 
                {
                    Graphics.FromImage(immagine).CopyFromScreen(new Point(rec.Left, rec.Top), Point.Empty, rec.Size);
                    connIngresso.Screen = JsonSerializer.Serialize<Image>(immagine);
                    connIngresso = paginaConn.serv.POST(connIngresso);
                    

                    /*esegue operazione a seconda del comando*/
                    switch (connIngresso.Command) 
                    {
                        case "FINE":
                            /*termina la comunicazione col client*/
                            break;

                        default:
                            if(connIngresso.Command != null) { COMANDO = JsonSerializer.Deserialize<EventArgs>(connIngresso.Command); }
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
    }
}
