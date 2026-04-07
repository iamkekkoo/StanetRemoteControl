using Microsoft.AspNet.SignalR.Client;
using Microsoft.MixedReality.WebRTC;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Asn1.Cms;
using StanetRemoteControl.RemoteControlNode;
using System;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;



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
            HubSignaling = connection.CreateHubProxy("SignalingHub");

            // Hub ON
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

                HubSignaling.On<string, string>("ReceiveAnswer", async (connId, sdp) =>
                {
                    Console.WriteLine("Answer ricevuta da controllato: " + sdp);
                    var msg = new SdpMessage
                    {
                        Type = SdpMessageType.Answer,
                        Content = sdp
                    };
                    try { pc.SetRemoteDescriptionAsync(msg); connessioneStabilita = true; } catch (Exception e) { throw e; }
                });

                HubSignaling.On<string, IceCandidate>("ReceiveIceCandidate", async (connId, candidate) =>
                {
                    Console.WriteLine("ICE inviata da controllato");
                    pc.AddIceCandidate(candidate);
                });
            }

            // riconnessione semplice a SignalR
            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubSchermo.Invoke("JoinConnection", mac.ID);
                    await HubPreciousCommand.Invoke("JoinConnection", mac.ID);
                    await HubMouseMove.Invoke("JoinConnection", mac.ID);
                    await HubSignaling.Invoke("JoinConnection", mac.ID);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            await connection.Start();
            await HubSchermo.Invoke("JoinConnection", mac.ID);
            await HubPreciousCommand.Invoke("JoinConnection", mac.ID);
            await HubMouseMove.Invoke("JoinConnection", mac.ID);
            await HubSignaling.Invoke("JoinConnection", mac.ID);


            // Avvia streaming TCP
            _ = StartStreaming(cts.Token);

            

            // Avvia timer ricezione comandi MouseMove
            StartMouseMoveCommandTimer();
        }

        private async Task StartStreaming(CancellationToken token)
        {
            
            while (!token.IsCancellationRequested)
            {
                if (connessioneStabilita) 
                { 
                    CaptureScreenBytes(schermo);

                    if (_lastArgb32Pixels != null)
                    {
                        _sendingFrame = true;
                        try
                        {
                            //HubSchermo.Invoke("SendScreen", mac.ID, _lastArgb32Pixels);


                            if (dataChannel != null && dataChannel.State == DataChannel.ChannelState.Open)
                            {
                                dataChannel.SendMessage(_lastArgb32Pixels);
                                Console.WriteLine("Schermo inviato");
                            }
                            Console.WriteLine("Schermo inviato");
                        }
                        finally
                        {
                            _sendingFrame = false;
                        
                        }
                    }
                }

                if (!connessioneStabilita) { try { pc.CreateOffer(); } catch (Exception e) { throw e; } }
                
                await Task.Delay(33); // 30 fps
               
                //Console.WriteLine("Cattura + Inoltro: " + stopwatch.ElapsedMilliseconds);
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
            if (connessioneStabilita) 
            {
                lock (_commandLock)
                {
                    if (_LatestMouseMoveCommandString == null)
                        return;

                    command = _LatestMouseMoveCommandString;
                }

                HandleCommand(command);
            }
            
        }

        /*Carica Utente + licenza*/
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

        /*CaricaMacchina, inizializza variabili per signaling UDP, Apre connessione a DB, chiama SignalR (TCP)*/
        async private void Controllato_Load(object sender, EventArgs e)
        {
            CaricaMacchina();
            await InitWebRTC();

            pc.LocalSdpReadytoSend += async (SdpMessage message) =>
            {
                if (message.Type == SdpMessageType.Offer)
                {
                    Task.Delay(33);
                    await HubSignaling.Invoke("SendOffer", mac.ID, message.Content);
                    Console.WriteLine("Offer inviata");
                }
            };

            pc.IceCandidateReadytoSend += async (IceCandidate candidate) =>
            {
                await HubSignaling.Invoke("SendIceCandidate", mac.ID, candidate);
                Console.WriteLine("ICE inviata da controllato");
            };

            connessione = Controllore.serv.ApriConnessione(mac);  //apre connessione a DB
            cts = new CancellationTokenSource();
            _ = InizializzaSignalR();
        }

        /*Inizializza PeerConnection per UDP*/
        async Task InitWebRTC()
        {
            pc = new PeerConnection();

            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer>
                {
                    new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                }
            };

            await pc.InitializeAsync(config);
            dataChannel = await pc.AddDataChannelAsync("pixel_stream", false, false);
            
        }

        /*Se non era già registrato carica l'utenza + sottoscrizione nell'applicazione*/
        private void button1_Click(object sender, EventArgs e)
        {
            new Thread(delegate ()
            {
                paginaConn = new Controllore();
                Application.EnableVisualStyles();
                paginaConn.IdMacchina = mac.ID;
                paginaConn.IdSottoscrizione = mac.IdSottoscrizione;
                Application.Run(paginaConn);
            }).Start();
        }

        private void CaptureScreenBytes(int monitorIndex)
        {
            var screen = Screen.AllScreens[monitorIndex];
            var bounds = screen.Bounds;

            if (_captureBitmap == null || _monitorBounds != bounds)
            {
                _captureGraphics?.Dispose();
                _captureBitmap?.Dispose();

                _captureBitmap = new Bitmap(
                    bounds.Width,
                    bounds.Height,
                    PixelFormat.Format32bppPArgb);

                _captureGraphics = Graphics.FromImage(_captureBitmap);
                _monitorBounds = bounds;
            }

            _captureGraphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

            // ⭐ SCALA IDEALE PER <500ms
            int thumbWidth = (int)(bounds.Width / ridimensionamento);
            int thumbHeight = (int)(bounds.Height / ridimensionamento);

            using (var thumb = new Bitmap(thumbWidth, thumbHeight, PixelFormat.Format32bppPArgb))
            using (Graphics g = Graphics.FromImage(thumb))
            {
                // ⚠️ NON usare HighQuality
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Low;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;

                g.DrawImage(_captureBitmap, 0, 0, thumbWidth, thumbHeight);


                

                using (var ms = new MemoryStream())
                {
                    thumb.Save(ms, JpegEnc, Jpeg35);

                    int len = (int)ms.Length;
                    byte[] result = new byte[16 + len];

                    Buffer.BlockCopy(BitConverter.GetBytes(DateTime.UtcNow.Ticks), 0, result, 0, 8);
                    Buffer.BlockCopy(BitConverter.GetBytes(thumbWidth), 0, result, 8, 4);
                    Buffer.BlockCopy(BitConverter.GetBytes(thumbHeight), 0, result, 12, 4);
                    Buffer.BlockCopy(ms.ToArray(), 0, result, 16, len);

                    // Aggiorna il delegate WebRTC (UDP)
                    lock (_frameLock)
                    {
                        int width = thumb.Width;
                        int height = thumb.Height;

                        _lastWidth = width;
                        _lastHeight = height;
                        _lastI420Frame = (in FrameRequest request) =>
                        {
                            unsafe
                            {
                                fixed (byte* ptr = result)
                                {
                                    Argb32VideoFrame frame = new Argb32VideoFrame
                                    {
                                        width = (uint)thumbWidth,
                                        height = (uint)thumbHeight,
                                        stride = _lastArgb32Stride,
                                        data = (IntPtr)ptr
                                    };
                                    
                                    request.Source.CompleteFrameRequest(
                                        request.RequestId,
                                        request.TimestampMs,
                                        in frame);
                                }
                            }
                        };
                    }

                    _lastArgb32Pixels = result;
                }
            }
        }

        private void HandleCommand(string cmdJson)
        {
            try
            {
                var mouseCmd = JsonConvert.DeserializeObject<MouseCommand>(cmdJson);
                mouseCmd.X = (int)(mouseCmd.X * ridimensionamento);
                mouseCmd.Y = (int)(mouseCmd.Y * ridimensionamento);
                if (mouseCmd != null && mouseCmd.Type == "MOUSE")
                {
                    if (mouseCmd.X == 0 && schermo - 1 >= 0) { schermo = schermo - 1; }
                    else if (mouseCmd.X == Screen.AllScreens[schermo].Bounds.Width && schermo + 1 < Screen.AllScreens.Length) { schermo = schermo + 1; }
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

        // ------------------- Win32 Imports -------------------
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        // ------------------- Costanti -------------------
        const int INPUT_KEYBOARD = 1;
        const uint KEYEVENTF_KEYUP = 0x0002;
        const uint KEYEVENTF_UNICODE = 0x0004;

        // ------------------- Structs -------------------
        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public InputUnion u;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct InputUnion
        {
            [FieldOffset(0)] public KEYBDINPUT ki;
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

        // ------------------- Funzioni Unicode -------------------
        static void KeyDownUnicode(char ch)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        static void KeyUpUnicode(char ch)
        {
            INPUT input = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }

        static void KeyPressUnicode(char ch)
        {
            KeyDownUnicode(ch);
            KeyUpUnicode(ch);
        }

        // ------------------- Mappa dei tasti -------------------
        static readonly Dictionary<string, char> KeyMap = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
{
    // LETTERE
    { "A",'A'},{ "B",'B'},{ "C",'C'},{ "D",'D'},{ "E",'E'},{ "F",'F'},{ "G",'G'},{ "H",'H'},{ "I",'I'},{ "J",'J'},
    { "K",'K'},{ "L",'L'},{ "M",'M'},{ "N",'N'},{ "O",'O'},{ "P",'P'},{ "Q",'Q'},{ "R",'R'},{ "S",'S'},{ "T",'T'},
    { "U",'U'},{ "V",'V'},{ "W",'W'},{ "X",'X'},{ "Y",'Y'},{ "Z",'Z'},

    // NUMERI
    { "0",'0'},{ "1",'1'},{ "2",'2'},{ "3",'3'},{ "4",'4'},{ "5",'5'},{ "6",'6'},{ "7",'7'},{ "8",'8'},{ "9",'9'},

    // TASTI SPECIALI
    { "ENTER", '\r'},
    { "TAB", '\t'},
    { "SPACE",' '},
    { "BACKSPACE", (char)8},
    { "ESC", (char)27},

    // MODIFICATORI (gestiti a parte)
    { "SHIFT", (char)0},
    { "CTRL", (char)0},
    { "CONTROL", (char)0},
    { "ALT", (char)0}
};

        // ------------------- Funzioni Modificatori -------------------
        static void KeyDownModifier(string mod)
        {
            switch (mod)
            {
                case "SHIFT": KeyDownUnicode((char)0); break;
                case "CTRL":
                case "CONTROL": KeyDownUnicode((char)0); break;
                case "ALT": KeyDownUnicode((char)0); break;
            }
        }

        static void KeyUpModifier(string mod)
        {
            switch (mod)
            {
                case "SHIFT": KeyUpUnicode((char)0); break;
                case "CTRL":
                case "CONTROL": KeyUpUnicode((char)0); break;
                case "ALT": KeyUpUnicode((char)0); break;
            }
        }

        // ------------------- Mappatura Unicode automatica -------------------
        static char MapKeyToChar(string key)
        {
            if (KeyMap.ContainsKey(key))
                return KeyMap[key];

            // Lettere e numeri maiuscole automatici
            if (!string.IsNullOrEmpty(key))
                return key.ToUpper()[0];

            return '\0';
        }

        // ------------------- Funzione EseguiKey -------------------
        static void EseguiKey(KeyCommand command)
        {
            if (command.Type != "KEY" || string.IsNullOrEmpty(command.Key))
                return;

            string key = command.Key.ToUpper();

            // ---------------- MODIFICATORI ----------------
            if (key == "CTRL" || key == "CONTROL")
            {
                ctrlDown = command.Action != "UP";
                return;
            }

            if (key == "SHIFT")
            {
                shiftDown = command.Action != "UP";
                return;
            }

            if (key == "ALT")
            {
                altDown = command.Action != "UP";
                return;
            }

            // ---------------- TASTO NORMALE ----------------

            if (command.Action == "DOWN")
                return; // con SendKeys non gestiamo DOWN separato

            string combo = "";

            if (ctrlDown) combo += "^";
            if (shiftDown) combo += "+";
            if (altDown) combo += "%";

            combo += FormatSpecial(key);

            SendKeys.SendWait(combo);
        }

        static string ConvertToSendKeys(string key)
        {
            key = key.ToUpper();

            if (key.Contains("+"))
            {
                var parts = key.Split('+');
                string result = "";

                foreach (var part in parts)
                {
                    switch (part)
                    {
                        case "CTRL":
                        case "CONTROL": result += "^"; break;
                        case "SHIFT": result += "+"; break;
                        case "ALT": result += "%"; break;
                        default:
                            result += FormatSpecial(part);
                            break;
                    }
                }

                return result;
            }

            return FormatSpecial(key);
        }

        static string FormatSpecial(string key)
        {
            key = key.ToUpper();
            Console.WriteLine(key);
            switch (key)
            {
                case "BACK": return "\b";
                case "ENTER": return "{ENTER}";
                case "TAB": return "{TAB}";
                case "ESC": return "{ESC}";
                case "BACKSPACE": return "{BACKSPACE}";
                case "DELETE": return "{DELETE}";
                case "SPACE": return " ";
                case "LEFT": return "{LEFT}";
                case "RIGHT": return "{RIGHT}";
                case "UP": return "{UP}";
                case "DOWN": return "{DOWN}";
                case "F1": return "{F1}";
                case "F2": return "{F2}";
                case "F3": return "{F3}";
                case "F4": return "{F4}";
                case "F5": return "{F5}";
                case "F6": return "{F6}";
                case "F7": return "{F7}";
                case "F8": return "{F8}";
                case "F9": return "{F9}";
                case "F10": return "{F10}";
                case "F11": return "{F11}";
                case "F12": return "{F12}";
                default:
                    return key.Length == 1 ? key : "";
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
