using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Newtonsoft.Json;
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
                    connIngresso.Screen = Newtonsoft.Json.JsonConvert.SerializeObject(immagine);
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
                                if (connIngresso.Command.CompareTo("FINE") != 0)
                                { COMANDO = System.Text.Json.JsonSerializer.Deserialize<EventArgs>(connIngresso.Command); }
                                else { this.Dispose(); }
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
    }
}
