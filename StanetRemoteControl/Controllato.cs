using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using NPOI.Util;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
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
        private async Task InizializzaSignalR()
        {
            connection = new HubConnection("https://francescostdev.azurewebsites.net");
            hub = connection.CreateHubProxy("ScreenHub");

            // Ricezione comandi
            hub.On<string>("ReceiveCommand", cmdJson =>
            {
                Task.Run(() => HandleCommand(cmdJson));
            });

            // Riconnessione
            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await hub.Invoke("JoinConnection", connessione.ID);
                }
                catch { }
            };

            await connection.Start();
            await hub.Invoke("JoinConnection", connessione.ID);

            // Avvia streaming
            _ = StartStreaming(cts.Token);
        }

        private async Task StartStreaming(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (connection.State == ConnectionState.Connected)
                {
                    try
                    {
                        var screenBytes = CaptureScreenBytes(schermo);
                        if (screenBytes != null && screenBytes.Length > 0)
                        {
                            await hub.Invoke("SendScreen", connessione.ID, screenBytes);
                        }
                    }
                    catch { /* ignora eccezioni temporanee */ }
                }

                await Task.Delay(16); // 60 fps
            }
        }

        private void CaricaMacchina()
        {
            // Lettura
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StanetRemoteControl");
            var filename = Path.Combine(dir, "Macchina.txt");
            
            
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(filename))
                File.WriteAllText(filename, "");

            string macchina = File.ReadAllText(filename);

            // Scrittura
            if (string.IsNullOrEmpty(macchina))
            {
                mac = Controllore.serv.RegistraMacchina(new Macchina());
                File.WriteAllText(filename, JsonConvert.SerializeObject(mac));
            }
            else
            {
                mac = JsonConvert.DeserializeObject<Macchina>(macchina);
            }

            MioId = mac.UserName;
            MiaPsw = mac.Password;
        }

        private void Controllato_Load(object sender, EventArgs e)
        {
            CaricaMacchina();

            connessione = Controllore.serv.ApriConnessione(mac);
            cts = new CancellationTokenSource();
            _ = InizializzaSignalR();
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

        private byte[] CaptureScreenBytes(int monitorIndex)
        {
            try
            {
                Screen[] screens = Screen.AllScreens;

                if (monitorIndex < 0 || monitorIndex >= screens.Length)
                    return null; // indice non valido

                // Spazio virtuale totale
                Rectangle virtualBounds = SystemInformation.VirtualScreen;

                // Monitor scelto
                Screen screen = screens[monitorIndex];
                Rectangle bounds = screen.Bounds;

                // Bitmap grande quanto lo spazio virtuale
                using (var bmp = new Bitmap(virtualBounds.Width, virtualBounds.Height, PixelFormat.Format24bppRgb))
                using (var g = Graphics.FromImage(bmp))
                {
                    // Copia tutto lo spazio virtuale
                    g.CopyFromScreen(virtualBounds.Left, virtualBounds.Top, 0, 0, virtualBounds.Size, CopyPixelOperation.SourceCopy);

                    // Ritaglia la porzione del monitor scelto
                    using (Bitmap monitorBmp = bmp.Clone(new Rectangle(bounds.X - virtualBounds.X, bounds.Y - virtualBounds.Y, bounds.Width, bounds.Height), bmp.PixelFormat))
                    using (var ms = new MemoryStream())
                    {
                        var enc = ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
                        var ep = new EncoderParameters(1);
                        ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);

                        monitorBmp.Save(ms, enc, ep);
                        return ms.ToArray();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private void HandleCommand(string cmdJson)
        {
            try
            {
                var mouseCmd = JsonConvert.DeserializeObject<MouseCommand>(cmdJson);
                if (mouseCmd != null && mouseCmd.Type == "MOUSE")
                {
                    if (mouseCmd.X > 0) { 
                    mouseCmd.X = mouseCmd.X * Screen.PrimaryScreen.Bounds.Width / 1000;
                    mouseCmd.Y = mouseCmd.Y * Screen.PrimaryScreen.Bounds.Height / 1000;
                    EseguiMouse(mouseCmd);
                    }
                    else { schermo = -mouseCmd.X; }
                        return;
                }

                var keyCmd = JsonConvert.DeserializeObject<KeyCommand>(cmdJson);
                if (keyCmd != null && keyCmd.Type == "KEY")
                {
                    EseguiKey(keyCmd);
                    return;
                }
            }
            catch { }
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
            return (byte)(int)Enum.Parse(typeof(Keys), key, true);
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
