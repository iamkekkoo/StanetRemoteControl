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

            
            /*crea la pagina e la inserisce nell'applicazione corrente*/
            Controllato pagina = new Controllato();

            Application.EnableVisualStyles();

            /*L'esecuzione delle operazioni ha inizio*/
            pagina.inizioControllato.Start();

            Application.Run(pagina);

            
        }
    }
}
