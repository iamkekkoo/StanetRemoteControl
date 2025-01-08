using StanetRemoteControl.RemoteControlNode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Drawing;
using System.Threading;

namespace StanetRemoteControl
{
    internal static class Program
    {
        

        /// <summary>
        /// Punto di ingresso principale dell'applicazione.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetCompatibleTextRenderingDefault(false);

            IService1 serv = new RemoteControlNode.Service1Client();
            Macchina mac = new Macchina();
            Connessione conn = new Connessione();
            Form1 pagina = new Form1();

            Rectangle rec = new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Bitmap immagine = new Bitmap(rec.Width, rec.Height);

            string COMANDO;
            /*-----------------------*/
            /*Macchina*/
            /*Verifica se già presente utenza in memoria*/
            FileStream fs;
            try
            {
                fs = File.Open("C:\\Program Files\\StanetRemoteControl\\Macchina.txt", FileMode.OpenOrCreate);
                fs.Dispose();
                mac = JsonSerializer.Deserialize<Macchina>(File.ReadAllText("C:\\Program Files\\StanetRemoteControl\\Macchina.txt"));

                if (mac == null)
                {
                    //Se non presente la registra
                    mac = serv.RegistraMacchina(new Macchina());
                    File.WriteAllText("C:\\Program Files\\StanetRemoteControl\\Macchina.txt", JsonSerializer.Serialize<Macchina>(mac));
                }
            }
            catch (Exception e)
            {
                conn.Error = e.Message;
                pagina.Errore = e.Message;
            }

            /*Connessione*/
            try
            {
                if (mac != new Macchina()) { conn = serv.ApriConnessione(mac); } //Apre Connessione solo se la macchina esiste (no eccezioni)
            }
            catch (Exception e)
            {
                conn.Error = e.Message;
                pagina.Errore = e.Message;
            }
            /*-----------------------*/

            pagina.MioId = mac.User;
            pagina.MiaPsw = mac.Password;

            conn.Command = "";

            /*Se la connessione è disponibile parte il thread di condivisione dello schermo (e lettura dei comandi)*/
            new Thread(delegate() 
            {
                while (conn.Command.CompareTo("FINE") != 0) 
                {
                    Graphics.FromImage(immagine).CopyFromScreen(new Point(rec.Left, rec.Top), Point.Empty, rec.Size);
                    conn.Screen = immagine;
                    conn = serv.POST(conn);
                    COMANDO = conn.Command;

                    /*esegue operazione a seconda del comando*/
                    switch (COMANDO) 
                    {
                        case "":
                            break;

                        default:
                            break;
                    }

                }
            }).Start();


            Application.EnableVisualStyles();
            
            Application.Run(pagina);
        }
    }
}
