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
            mac.User = "";
            mac.Password = "";
            immagine = new Bitmap(rec.Width, rec.Height);

            /*Se la connessione è disponibile parte il thread di condivisione dello schermo (e lettura dei comandi)*/
            new Thread(delegate() 
            {
                var b = true;
                while ( b ) 
                {
                    Graphics.FromImage(immagine).CopyFromScreen(new Point(rec.Left, rec.Top), Point.Empty, rec.Size);
                    connIngresso.Screen = JsonSerializer.Serialize<Image>(immagine);
                    connIngresso = paginaConn.serv.POST(connIngresso);

                    /*esegue operazione a seconda del comando*/
                    switch (connIngresso.Command) 
                    {
                        case "FINE":
                            /*termina la comunicazione col client*/
                            b = false;
                            break;

                        default:
                            if(connIngresso.Command != null && connIngresso.Command.CompareTo("") != 0) { COMANDO = JsonSerializer.Deserialize<EventArgs>(connIngresso.Command); }
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
