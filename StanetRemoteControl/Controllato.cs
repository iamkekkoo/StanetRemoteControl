using Newtonsoft.Json;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
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

            this.Load += Controllato_Load;

            return;
        }

        private void CaricaMacchina()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StanetRemoteControl");
            string filename = Path.Combine(dir, "Macchina.txt");

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filename))
                File.WriteAllText(filename, "");

            // Lettura
            string macchina = File.ReadAllText(filename);

            if (string.IsNullOrEmpty(macchina))
            {
                mac = Controllore.serv.RegistraMacchina(new Macchina());
                File.WriteAllText(filename, JsonConvert.SerializeObject(mac));
            }
            else
            {
                mac = JsonConvert.DeserializeObject<Macchina>(macchina);
            }

            MioId = mac.User;
            MiaPsw = mac.Password;
        }

        private void Controllato_Load(object sender, EventArgs e)
        {
            CaricaMacchina();
            _ = InizioControllatoAsync();  // ✔️ ORA È SICURO!
        }

        //Thread di connessione lato controllato
        /*Thread di avvio del controllato*/
        public async Task InizioControllatoAsync()
        {
            // Prima connessione
            Connessione connessione = new Connessione();
            string attesa = "";

            // Ciclo principale per gestire multiconnessioni lato ricevitore
            while (attivo)
            {
                connessione = Controllore.serv.ApriConnessione(mac);

                // Avvia connessione in modo asincrono
                var taskConn = Task.Run(() => avviaConnessione(connessione));
                connIngresso.Add((connessione, taskConn));

                // Ciclo interno per POST finché ID_Controllore != 0
                do
                {
                    attesa = Controllore.serv.POST(BitConverter.GetBytes(1), connessione.ID);

                    // Non blocca il thread, solo attende in modo asincrono
                    await Task.Delay(400);
                }while (string.IsNullOrEmpty(attesa) || attesa.CompareTo("3000") != 0) ;
                
                // Task già avviato con Task.Run, non serve Start
            }
        }


        public async Task avviaConnessione(Connessione conn)
        {
            bool b = true;
            while (b)
            {
                // Cattura dello schermo
                var screenshot = CapturePrimaryScreen();

                // CLONA IL BITMAP prima di assegnarlo alla UI
                var safeImage = (Bitmap)screenshot.Clone();

                // CLONA IL BITMAP prima di assegnarlo alla UI
                var sendImage = (Bitmap)screenshot.Clone();

                // Aggiornamento UI in modo thread-safe
                if (pictureBox1.InvokeRequired)
                {
                    pictureBox1.Invoke(new Action(() =>
                    {
                        pictureBox1.Image = safeImage;
                    }));
                }
                else
                {
                    pictureBox1.Image = safeImage;
                }

                // Invia screenshot al server e riceve comando
                conn.Command = await Task.Run(() => Controllore.serv.POST(BitmapToBytes(sendImage), conn.ID));

                if (!string.IsNullOrEmpty(conn.Command))
                {
                    if (conn.Command.CompareTo("FINE") == 0)
                    {
                        // comunicazione terminata dal controllore
                        b = false;
                    }
                    else
                    {
                        // Tentativo di deserializzare in MouseCommand
                        try
                        {
                            var mouseCmd = JsonConvert.DeserializeObject<MouseCommand>(conn.Command);
                            if (mouseCmd != null && mouseCmd.Type == "MOUSE")
                            {
                                EseguiMouse(mouseCmd);
                                continue;
                            }
                        }
                        catch { }

                        // Tentativo di deserializzare in KeyCommand
                        try
                        {
                            var keyCmd = JsonConvert.DeserializeObject<KeyCommand>(conn.Command);
                            if (keyCmd != null && keyCmd.Type == "KEY")
                            {
                                EseguiKey(keyCmd);
                                continue;
                            }
                        }
                        catch { }
                    }
                }

                // Attesa asincrona di 400 ms senza bloccare il thread
                await Task.Delay(400);
            }
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

        static Bitmap CapturePrimaryScreen()
        {
            var screen = Screen.PrimaryScreen;
            var bmp = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(
                    screen.Bounds.Left,
                    screen.Bounds.Top,
                    0,
                    0,
                    screen.Bounds.Size
                );
                g.Flush();
            }
            
            return bmp;
        }

        public static byte[] BitmapToBytes(Bitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
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

        static void EseguiMouse(MouseCommand cmd)
        {
            if (cmd.Action == "MOVE")
            {
                Cursor.Position = new System.Drawing.Point(cmd.X, cmd.Y);
            }
            else
            {
                // API Win32 per click
                mouse_event(GetMouseEvent(cmd.Action), 0, 0, 0, (UIntPtr)0);
            }
        }

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        static uint GetMouseEvent(string action)
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

        static void EseguiKey(KeyCommand cmd)
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
        static byte ConvertKey(string key)
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
