using StanetRemoteControl.RemoteControlNode;
using System;
using System.Windows.Forms;
using System.Text.Json;
using System.IO;
using System.Drawing;
using System.Threading;

using System.Text;
using System.Net.Http.Headers;
using System.Security;

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
            /*Variabili*/
            Application.SetCompatibleTextRenderingDefault(false);

            Form1 pagina = new Form1();
            FormConnessione paginaConn = new FormConnessione();
            IService1 serv = new RemoteControlNode.Service1Client();

            Rectangle rec = new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Bitmap immagine = new Bitmap(rec.Width, rec.Height);

            string COMANDO;

            FileStream fs;
            StreamReader reader;
            string macchina;

            string path = AppDomain.CurrentDomain.BaseDirectory;
            string dir = path + "\\StanetRemoteControl";
            string filename = dir + "\\Macchina.txt";
            /*-----------------------*/



            if (!Directory.Exists(dir)) 
            {
                Directory.CreateDirectory(dir);
            }
            fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            reader = new StreamReader(fs);
            macchina = reader.ReadToEnd();

            /*Macchina*/
            /*Verifica se già presente utenza in memoria*/
            if (macchina.CompareTo("") == 0)
            {
                //file appena creato -> vuoto
                //Se non presente la registra


                /*pagina.mac = serv.RegistraMacchina(new Macchina());*/
                pagina.mac = new Macchina();
                
                
                
                
                
                //File.WriteAllText(filename, JsonSerializer.Serialize<Macchina>(pagina.mac));
            }
            else 
            {
                /*altrimenti utilizza la macchina registrata*/
                pagina.mac = JsonSerializer.Deserialize<Macchina>(macchina);
            }
                

            reader.Dispose();
            fs.Dispose();
             
           

            /*Connessione*/
            try
            {
                if (pagina.mac.ID != 0) { paginaConn.connIngresso = serv.ApriConnessione(pagina.mac); } //Apre Connessione solo se la macchina esiste (no eccezioni)
            }
            catch (Exception e)
            {
                paginaConn.connIngresso.Error = e.Message;
                pagina.Errore = "2 --- " + e.Message;
            }
            /*-----------------------*/

            pagina.MioId = pagina.mac.User;
            pagina.MiaPsw = pagina.mac.Password;

            paginaConn.connIngresso.Command = "";

            /*Se la connessione è disponibile parte il thread di condivisione dello schermo (e lettura dei comandi)*/
            /*new Thread(delegate() 
            {
                while (pagina.conn.Command.CompareTo("FINE") != 0) 
                {
                    Graphics.FromImage(immagine).CopyFromScreen(new Point(rec.Left, rec.Top), Point.Empty, rec.Size);
                    pagina.conn.Screen = JsonSerializer.Serialize<Image>(immagine);
                    pagina.conn = pagina.serv.POST(pagina.conn);
                    COMANDO = pagina.conn.Command;

                    esegue operazione a seconda del comando
                    switch (COMANDO) 
                    {
                        case "":
                            break;

                        default:
                            break;
                    }

                }
            }).Start();*/

            Application.EnableVisualStyles();
            
            Application.Run(pagina);
        }
    }
}
