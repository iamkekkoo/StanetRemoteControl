using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using NPOI.Util;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Collections.Generic;
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
                    else { schermo = schermo -1; }
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


        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);


        [DllImport("user32.dll")]
        static extern uint MapVirtualKey(uint uCode, uint uMapType);

        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_SCANCODE = 0x0008;
        const uint KEYEVENTF_EXTENDEDKEY = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public int type;
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        static readonly HashSet<ushort> PressedKeys = new HashSet<ushort>();
        static readonly object KeyLock = new object();


        static void EseguiKey(KeyCommand cmd)
        {
            if (cmd == null || cmd.Type != "KEY")
                return;

            List<ushort> keys = cmd.Key
                .Split('+')
                .Select(k => ParseVirtualKey(k))
                .ToList();

            if (cmd.Action == "DOWN")
            {
                SendKeys(keys, false);
            }
            else if (cmd.Action == "UP")
            {
                SendKeys(keys, true);
            }
            else if (cmd.Action == "PRESS")
            {
                SendKeys(keys, false);
                SendKeys(keys, true);
            }
        }

        static void SendKeys(List<ushort> keys, bool keyUp)
        {
            IEnumerable<ushort> ordered = keyUp ? keys.AsEnumerable().Reverse() : keys;
            List<INPUT> inputs = new List<INPUT>();

            lock (KeyLock)
            {
                foreach (ushort vk in ordered)
                {
                    if (!keyUp && PressedKeys.Contains(vk))
                        continue;

                    if (keyUp && !PressedKeys.Contains(vk))
                        continue;

                    INPUT input = new INPUT();
                    input.type = INPUT_KEYBOARD;
                    input.ki.wVk = vk;
                    input.ki.wScan = 0;
                    input.ki.dwFlags = keyUp ? KEYEVENTF_KEYUP : 0;
                    input.ki.time = 0;
                    input.ki.dwExtraInfo = IntPtr.Zero;

                    inputs.Add(input);

                    if (keyUp)
                        PressedKeys.Remove(vk);
                    else
                        PressedKeys.Add(vk);
                }
            }

            if (inputs.Count > 0)
                SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        static void ReleaseAllKeys()
        {
            lock (KeyLock)
            {
                foreach (ushort vk in PressedKeys.ToList())
                {
                    INPUT input = new INPUT();
                    input.type = INPUT_KEYBOARD;
                    input.ki.wVk = vk;
                    input.ki.dwFlags = KEYEVENTF_KEYUP;

                    SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
                }

                PressedKeys.Clear();
            }
        }

        static ushort ParseVirtualKey(string key)
        {
            key = key.Trim().ToUpper();

            switch (key)
            {
                case "CTRL":
                case "CONTROL": return 0x11;
                case "SHIFT": return 0x10;
                case "ALT": return 0x12;
                case "ENTER": return 0x0D;
                case "ESC":
                case "ESCAPE": return 0x1B;
                case "TAB": return 0x09;
                case "SPACE": return 0x20;

                case "LEFT": return 0x25;
                case "UP": return 0x26;
                case "RIGHT": return 0x27;
                case "DOWN": return 0x28;

                case "DELETE":
                case "DEL": return 0x2E;
                case "INSERT":
                case "INS": return 0x2D;
                case "HOME": return 0x24;
                case "END": return 0x23;
                case "PAGEUP": return 0x21;
                case "PAGEDOWN": return 0x22;

                // 🔊 MULTIMEDIA
                case "VOLUMEUP": return 0xAF;
                case "VOLUMEDOWN": return 0xAE;
                case "MUTE": return 0xAD;
                case "PLAY":
                case "PLAYPAUSE": return 0xB3;
                case "STOP": return 0xB2;
                case "NEXT": return 0xB0;
                case "PREV":
                case "PREVIOUS": return 0xB1;

                default:
                    return (ushort)Enum.Parse(typeof(Keys), key, true);
            }
        }


        static void Send(List<Keys> keys, bool keyUp)
        {
            var ordered = keyUp ? keys.AsEnumerable().Reverse() : keys;
            var inputs = new List<INPUT>();

            foreach (var key in ordered)
            {
                ushort scanCode = (ushort)MapVirtualKey((uint)key, 0);

                uint flags = KEYEVENTF_SCANCODE;
                if (keyUp) flags |= KEYEVENTF_KEYUP;

                // tasti estesi (frecce, INS, DEL, HOME, END, ecc.)
                if (IsExtendedKey(key))
                    flags |= KEYEVENTF_EXTENDEDKEY;

                inputs.Add(new INPUT
                {
                    type = INPUT_KEYBOARD,
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = scanCode,
                        dwFlags = flags,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                });
            }

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf(typeof(INPUT)));
        }

        static bool IsExtendedKey(Keys key)
        {
            switch (key)
            {
                case Keys.Insert:
                case Keys.Delete:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Right:
                case Keys.Left:
                case Keys.Up:
                case Keys.Down:
                case Keys.NumLock:
                case Keys.PrintScreen:
                case Keys.Divide:
                    return true;
                default:
                    return false;
            }
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
