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
    public partial class Form1 : Form
    {
        public Form1()
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
                    COMANDO = connIngresso.Command;

                    /*esegue operazione a seconda del comando*/
                    switch (COMANDO) 
                    {
                        case "FINE":
                            /*termina la comunicazione col client*/
                            break;

                        default:
                            break;
                    }

                }
            }).Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(delegate ()
            {
                paginaConn = new FormConnessione();
                Application.EnableVisualStyles();
                Application.Run(paginaConn);
            }).Start();
        }
    }
}
