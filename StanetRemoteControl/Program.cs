using StanetRemoteControl.RemoteControlNode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

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
            Macchina mac = serv.RegistraMacchina(new Macchina());
            Connessione con = serv.ApriConnessione(mac);


            Form1 pagina = new Form1();
            pagina.MioId = mac.User;
            pagina.MiaPsw = mac.Password;

            Application.EnableVisualStyles();
            
            Application.Run(pagina);
        }
    }
}
