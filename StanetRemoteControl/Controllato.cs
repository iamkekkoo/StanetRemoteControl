using Newtonsoft.Json;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace StanetRemoteControl
{
    public partial class Controllato : Form
    {
        public Controllato()
        {
            InitializeComponent();
            byte[] bx;

            //Thread di connessione lato controllato
            Thread avviaConnessione = new Thread(delegate ()
                {
                var b = true;
                while (b)
                {
                    //Graphics.FromImage(immagine).CopyFromScreen(0, 0, immagine.Width, immagine.Height, immagine.Size);
                    bx = CaptureFullScreen();
                    connIngresso[id].conn.Command = Controllore.serv.POST(bx, connIngresso[ThreadID].conn.ID);
                    /*esegue operazione a seconda del comando*/
                    if (connIngresso[ThreadID].conn.Command != null)
                    {
                        if (connIngresso[ThreadID].conn.Command.CompareTo("FINE") == 0)
                        {
                            /*comunicazione terminata dal controllore*/
                            b = false;
                        }
                        else
                        {
                            /*prova a deserializzare in tutti i tipi possibili (in caso di eccezione passa al tipo successivo)*/
                            try
                            {
                                // Tentativo: comando di tipo MOUSE
                                var mouseCmd = JsonConvert.DeserializeObject<MouseCommand>(connIngresso[ThreadID].conn.Command);
                                if (mouseCmd != null && mouseCmd.Type == "MOUSE")
                                {
                                    EseguiMouse(mouseCmd);
                                    continue;
                                }
                            }
                            catch (Exception e) { }

                            try
                            {
                                // Tentativo: comando di tipo KEY
                                var keyCmd = JsonConvert.DeserializeObject<KeyCommand>(connIngresso[ThreadID].conn.Command);
                                if (keyCmd != null && keyCmd.Type == "KEY")
                                {
                                    EseguiKey(keyCmd);
                                    continue;
                                }
                            }
                            catch (Exception e) { }
                        }
                    }
                }
            });

            /*Thread di avvio del controllato*/
            Thread inizioControllato = new Thread(delegate ()
            {
                /*il thread aggiunge una connessione allo stack ogni volta che l'ultima dello stack viene impegnata (multiconnessione lato ricevitore)*/
                do
                {
                    connIngresso.Add((Controllore.serv.ApriConnessione(mac), avviaConnessione));

                    do
                    {
                        Controllore.serv.POST(BitConverter.GetBytes(1), connIngresso[connIngresso.Count - 1].conn.ID);
                    } while (connIngresso[connIngresso.Count - 1].conn.ID_Controllore != 0);

                    connIngresso[connIngresso.Count - 1].t.Start();

                } while (attivo);
            });

            /*cerca macchina registrata*/
            string dir = AppDomain.CurrentDomain.BaseDirectory + "StanetRemoteControl";
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
                mac = Controllore.serv.RegistraMacchina(new Macchina());
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

            return;
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

        public byte[] CaptureFullScreen()
        {
            // Ottieni risoluzione schermo
            int width = Screen.PrimaryScreen.Bounds.Width;
            int height = Screen.PrimaryScreen.Bounds.Height;

            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return ms.ToArray();
                }
            }
        }


        public byte[] imageToByteArray(Image imageIn)
        {
            MemoryStream ms = new MemoryStream();
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
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


        void EseguiMouse(MouseCommand cmd)
        {
            if (cmd.Action == "MOVE")
            {
                Cursor.Position = new Point(cmd.X, cmd.Y);
            }
            else
            {
                // API Win32 per click
                mouse_event(GetMouseEvent(cmd.Action), 0, 0, 0, (UIntPtr)0);
            }
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        uint GetMouseEvent(string action)
        {
            switch (action)
            {
                case "LEFT_DOWN": return 0x0002;
                case "LEFT_UP": return 0x0004;
                case "RIGHT_DOWN": return 0x0008;
                case "RIGHT_UP": return 0x0010;
                case "MIDDLE_DOWN": return 0x0020;
                case "MIDDLE_UP": return 0x0040;
                default: return 0;
            }
        }

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

        void EseguiKey(KeyCommand cmd)
        {
            byte key = ConvertKey(cmd.Key);

            if (cmd.Action == "DOWN")
                keybd_event(key, 0, 0, 0);
            else if (cmd.Action == "UP")
                keybd_event(key, 0, 2, 0);
            else if (cmd.Action == "PRESS")
            {
                keybd_event(key, 0, 0, 0);
                keybd_event(key, 0, 2, 0);
            }
        }

        // Mappa basilare, si può estendere
        byte ConvertKey(string key)
        {
            return (byte)Keys.Parse(typeof(Keys), key, true);
        }

    }
    public class MouseCommand
    {
        public string Type { get; set; }  // "MOUSE"
        public int X { get; set; }
        public int Y { get; set; }
        public string Action { get; set; }  // "MOVE", "LEFT_DOWN", "LEFT_UP", etc.
    }

    public class KeyCommand
    {
        public string Type { get; set; }  // "KEY"
        public string Key { get; set; }   // "A", "ENTER", "CTRL+C", ecc.
        public string Action { get; set; } // "DOWN", "UP", "PRESS"
    }

}
