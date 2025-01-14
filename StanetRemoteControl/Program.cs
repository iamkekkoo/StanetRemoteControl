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

            FileStream fs;
            StreamReader reader;
            string macchina;

            string path = AppDomain.CurrentDomain.BaseDirectory;
            string dir = path + "\\StanetRemoteControl";
            string filename = dir + "\\Macchina.txt";
            /*-----------------------*/


            /*Lavoro sulle directory per leggere il file Macchina*/
            if (!Directory.Exists(dir)) 
            {
                Directory.CreateDirectory(dir);
            }
            fs = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            reader = new StreamReader(fs);
            macchina = reader.ReadToEnd();

            
            Controllato pagina = new Controllato(macchina);

            /*Qui devo registrare la macchina creata da pagina su file*/

            /*--------------------------------------------------------*/

            reader.Dispose();
            fs.Dispose();
             
            pagina.MioId = pagina.mac.User;
            pagina.MiaPsw = pagina.mac.Password;
                      

            Application.EnableVisualStyles();            
            Application.Run(pagina);
        }
    }
}
