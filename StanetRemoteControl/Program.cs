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

            Controllato pagina = new Controllato();
            
            

            

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


                pagina.mac = pagina.paginaConn.serv.RegistraMacchina(pagina.mac);
                
                
                
                
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
                if (pagina.mac.ID != 0) { pagina.connIngresso = pagina.paginaConn.serv.ApriConnessione(pagina.mac); } //Apre Connessione solo se la macchina esiste (no eccezioni)
            }
            catch (Exception e)
            {
                pagina.connIngresso.Error = e.Message;
                pagina.Errore = "2 --- " + e.Message;
            }
            /*-----------------------*/

            pagina.MioId = pagina.mac.User;
            pagina.MiaPsw = pagina.mac.Password;

            pagina.connIngresso.Command = "";

            

            Application.EnableVisualStyles();
            
            Application.Run(pagina);
        }
    }
}
