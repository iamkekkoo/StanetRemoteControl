using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using StanetRemoteControl.RemoteControlNode;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;

namespace StanetRemoteControl
{
    public partial class Controllato : Form
    {
        public Controllato()
        {
            InitializeComponent();
            connIngresso.Add(new Connessione());

            /*cerca macchina registrata*/
            string dir = AppDomain.CurrentDomain.BaseDirectory + "\\StanetRemoteControl";
            string filename = dir + "\\Macchina.txt";

            /*Lavoro sulle directory per leggere il file Macchina*/
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            /*Lettura*/
            fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Read);
            reader = new StreamReader(fs);
            macchina = reader.ReadToEnd();
            fs.Flush();
            reader.Dispose();
            fs.Dispose();

            /*Scrittura*/
            fs = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write);
            writer = new StreamWriter(fs);
            if (macchina.CompareTo("") == 0)
            {
                //file appena creato -> vuoto
                //Se non presente la registra
                mac = paginaConn.serv.RegistraMacchina(new Macchina());
                writer.Write(JsonConvert.SerializeObject(mac));
            }
            else
            {
                /*altrimenti utilizza la macchina registrata*/
                mac = JsonConvert.DeserializeObject<Macchina>(macchina);
            }
            fs.Flush();
            writer.Dispose();
            fs.Dispose();

            MioId = mac.User;
            MiaPsw = mac.Password;

            /*il thread avvia una nuova connessione ogni volta che viene chiusa la connessione con un client (max 1 controllore alla volta)*/
            new Thread(delegate ()
            {
                i = 0;
                while (attivo)
                {
                    while (connIngresso[i].ID_Controllore == 0)
                    {
                        connIngresso[i] = paginaConn.serv.ApriConnessione(mac);
                    }

                    new Thread(delegate ()
                    {
                        var k = i;

                        var b = true;
                        while (b)
                        {
                            Graphics.FromImage(immagine).CopyFromScreen(0, 0, 0, 0, immagine.Size);
                            connIngresso[k].Screen = imageToByteArray(immagine);
                            connIngresso[k].Command = paginaConn.serv.POST(connIngresso[k].Screen, connIngresso[k].ID);

                            /*esegue operazione a seconda del comando*/
                            if (connIngresso[k].Command != null)
                            {
                                if (connIngresso[k].Command.CompareTo("FINE") == 0)
                                {
                                    /*comunicazione terminata dal controllore*/
                                    b = false;
                                }
                                else
                                {
                                    /*prova a deserializzare in tutti i tipi possibili (in caso di eccezione passa al tipo successivo)*/
                                    try { Errore = connIngresso[k].Command; }
                                    catch (Exception e)
                                    {
                                        try { Errore = connIngresso[k].Command; }
                                        catch (Exception e2)
                                        {
                                            Errore = e2.Message;
                                        }
                                    }
                                }
                            }
                        }
                    }).Start();
                }

                connIngresso.Add(new Connessione());
                i++;
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

        private static byte[] imageToByteArray(System.Drawing.Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static byte[] Compress(byte[] data)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }
}
