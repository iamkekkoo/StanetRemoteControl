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

            /*Divido inoltro screen, ricezione comandi importanti (tastiera e MouseClick) e MouseMove in 3 hub differenti a differente gestione*/
            HubSchermo = connection.CreateHubProxy("ScreenHub");
            HubPreciousCommand = connection.CreateHubProxy("PreciousCommandHub");
            HubMouseMove = connection.CreateHubProxy("MouseMoveHub");

            // Ricezione comandi
            {
                /*Comandi importanti*/
                HubPreciousCommand.On<string>("ReceiveCommand", cmdJson => { HandleCommand(cmdJson); });

                /*Gestione comandi MouseMove (timer)*/
                HubMouseMove.On<string>("ReceiveCommand", cmdJson =>
                {
                    lock (_commandLock)
                    {
                        _LatestMouseMoveCommandString = cmdJson;
                    }
                });
            }

            // Riconnessione
            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await HubSchermo.Invoke("JoinConnection", mac.ID);
                }
                catch { }
            };
            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await HubPreciousCommand.Invoke("JoinConnection", mac.ID);
                }
                catch { }
            };
            connection.Closed += async () =>
            {
                await Task.Delay(2000);
                try
                {
                    await connection.Start();
                    await HubMouseMove.Invoke("JoinConnection", mac.ID);
                }
                catch { }
            };

            await connection.Start();
            await HubSchermo.Invoke("JoinConnection", mac.ID);
            await HubPreciousCommand.Invoke("JoinConnection", mac.ID);
            await HubMouseMove.Invoke("JoinConnection", mac.ID);

            // Avvia streaming
            _ = StartStreaming(cts.Token);

            // Avvia timer ricezione comandi MouseMove
            StartMouseMoveCommandTimer();
        }

        private async Task StartStreaming(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                screenBytes = CaptureScreenBytes(schermo);
                if (screenBytes != null)
                {
                    await HubSchermo.Invoke("SendScreen", mac.ID, screenBytes);
                }
                
                await Task.Delay(100); // 10 fps
            }
        }

        private void StartMouseMoveCommandTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 4; // ~240 FPS
            _renderTimer.Tick += RenderMouseMove;
            _renderTimer.Start();
        }

        private void RenderMouseMove(object sender, EventArgs e)
        {
            string command;

            lock (_commandLock)
            {
                if (_LatestMouseMoveCommandString == null)
                    return;

                command = _LatestMouseMoveCommandString;
            }

            HandleCommand(command);
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
                    return null;

                Screen screen = screens[monitorIndex];
                Rectangle bounds = screen.Bounds;

                if (_captureBitmap == null || _monitorBounds != bounds)
                {
                    _captureBitmap?.Dispose();
                    _captureBitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
                    _monitorBounds = bounds;
                }

                using (Graphics g = Graphics.FromImage(_captureBitmap))
                {
                    g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }

                using (var ms = new MemoryStream())
                {
                    var enc = ImageCodecInfo.GetImageEncoders().First(e => e.MimeType == "image/jpeg");
                    var ep = new EncoderParameters(1);
                    ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);

                    _captureBitmap.Save(ms, enc, ep);
                    byte[] jpegBytes = ms.ToArray();

                    byte[] result = new byte[8 + jpegBytes.Length];
                    Array.Copy(BitConverter.GetBytes(bounds.Width), 0, result, 0, 4);
                    Array.Copy(BitConverter.GetBytes(bounds.Height), 0, result, 4, 4);
                    Array.Copy(jpegBytes, 0, result, 8, jpegBytes.Length);

                    return result;
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
                    if (mouseCmd.X == 0) { schermo = schermo - 1; Task.Delay(1500); }
                    else if (mouseCmd.X == Screen.AllScreens[schermo].Bounds.Width) { schermo = schermo + 1; Task.Delay(1500); }
                    else {
                    EseguiMouse(mouseCmd);
                    }
                    return;
                }

                var keyCmd = JsonConvert.DeserializeObject<KeyCommand>(cmdJson);
                if (keyCmd != null && keyCmd.Type == "KEY")
                {
                    Console.WriteLine(cmdJson);
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
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;

        struct INPUT
        {
            public int type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
        }

        struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        // ------------------- Mappa tasti -------------------
        static readonly Dictionary<string, ushort> KeyMap = new Dictionary<string, ushort>()
        {
            { "A", (ushort)Keys.A },
            { "B", (ushort)Keys.B },
            { "C", (ushort)Keys.C },
            { "D", (ushort)Keys.D },
            { "E", (ushort)Keys.E },
            { "F", (ushort)Keys.F },
            { "G", (ushort)Keys.G },
            { "H", (ushort)Keys.H },
            { "I", (ushort)Keys.I },
            { "J", (ushort)Keys.J },
            { "K", (ushort)Keys.K },
            { "L", (ushort)Keys.L },
            { "M", (ushort)Keys.M },
            { "N", (ushort)Keys.N },
            { "O", (ushort)Keys.O },
            { "P", (ushort)Keys.P },
            { "Q", (ushort)Keys.Q },
            { "R", (ushort)Keys.R },
            { "S", (ushort)Keys.S },
            { "T", (ushort)Keys.T },
            { "U", (ushort)Keys.U },
            { "V", (ushort)Keys.V },
            { "W", (ushort)Keys.W },
            { "X", (ushort)Keys.X },
            { "Y", (ushort)Keys.Y },
            { "Z", (ushort)Keys.Z },
            { "ENTER", (ushort)Keys.Enter },
            { "ESC", (ushort)Keys.Escape },
            { "TAB", (ushort)Keys.Tab },
            { "SPACE", (ushort)Keys.Space },
            { "CTRL", (ushort)Keys.ControlKey },
            { "SHIFT", (ushort)Keys.ShiftKey },
            { "ALT", (ushort)Keys.Menu },
            { "F1", (ushort)Keys.F1 },
            { "F2", (ushort)Keys.F2 },
            { "F3", (ushort)Keys.F3 },
            { "F4", (ushort)Keys.F4 },
            { "F5", (ushort)Keys.F5 },
            { "F6", (ushort)Keys.F6 },
            { "F7", (ushort)Keys.F7 },
            { "F8", (ushort)Keys.F8 },
            { "F9", (ushort)Keys.F9 },
            { "F10", (ushort)Keys.F10 },
            { "F11", (ushort)Keys.F11 },
            { "F12", (ushort)Keys.F12 }
        };

        // ------------------- Funzioni invio tasto -------------------
        static void KeyDown(ushort key)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = key }
                }
            };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        static void KeyUp(ushort key)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT { wVk = key, dwFlags = KEYEVENTF_KEYUP }
                }
            };
            SendInput(1, new INPUT[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        static void KeyPress(ushort key)
        {
            KeyDown(key);
            KeyUp(key);
        }

        // ------------------- Gestione KeyCommand -------------------
        static void EseguiKey(KeyCommand command)
        {
            if (command.Type != "KEY" || string.IsNullOrEmpty(command.Key))
                return;

            string[] keys = command.Key.Split('+');

            // Premere modificatori
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (KeyMap.ContainsKey(keys[i].ToUpper()))
                    KeyDown(KeyMap[keys[i].ToUpper()]);
            }

            // Tasto principale
            string mainKeyStr = keys[keys.Length - 1].ToUpper();
            if (!KeyMap.ContainsKey(mainKeyStr))
                return;

            ushort mainKey = KeyMap[mainKeyStr];

            switch (command.Action.ToUpper())
            {
                case "DOWN":
                    KeyDown(mainKey);
                    break;
                case "UP":
                    KeyUp(mainKey);
                    break;
                default:
                case "PRESS":
                    KeyPress(mainKey);
                    break;
            }

            // Rilasciare modificatori
            for (int i = keys.Length - 2; i >= 0; i--)
            {
                if (KeyMap.ContainsKey(keys[i].ToUpper()))
                    KeyUp(KeyMap[keys[i].ToUpper()]);
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
