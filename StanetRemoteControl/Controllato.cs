using MathNet.Numerics.Optimization;
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
        //------------------------Funzionamento Base-----------------------------

        public Controllato()
        {
            InitializeComponent();
            this.Load += Controllato_Load;


            return;
        } //Costruttore 

        /*Carica i dati della macchina, apre la connessione al DB ed avvia le connessioni di trasmissione video e ricezione comandi*/
        async private void Controllato_Load(object sender, EventArgs e)
        {
            CaricaMacchina(); //Carica i dati della macchina

            connessione = Controllore.serv.ApriConnessione(mac);  //apre connessione a DB

            /*Avvia i servizi di trasmissione: SignalR e WebRTC*/
            _ = InizializzaSignalR(); //utilizzato prima per Signaling WebRTC e poi come trasmissione di backup
            _ = InizializzaWebRTC();

            
        }


        /*Carica in memoria i dati relativi all'utenza ed alla licenza della macchina locale*/
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
        
        
        
        //--------------------------SignalR e WebRTC-----------------------------

        /*Apre i canali di trasmissione video TCP e ricezione comandi*/
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

                HubSignaling.On<string, string>("ReceiveOffer", async (connId, sdp) =>
                {
                    Console.WriteLine("Offer ricevuta da controllato");
                    var msg = new SdpMessage
                    {
                        Type = SdpMessageType.Offer,
                        Content = sdp
                    };

                    await pc.SetRemoteDescriptionAsync(msg);

                    pc.CreateAnswer();
                });

                HubSignaling.On<string, IceCandidate>("ReceiveIceCandidate", async (connId, candidate) =>
                {
                    
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


            // Avvia timer ricezione comandi MouseMove
            StartMouseMoveCommandTimer();
        }

        async Task InizializzaWebRTC()
        {
            /*Creazione canale*/
            pc = new PeerConnection();

            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer>
                {
                    new IceServer{ Urls = { "stun:stun.cloudflare.com:3478" } }
                }
            };

            await pc.InitializeAsync(config);

            /*Aggiunge eventi per Signaling*/
            

            pc.LocalSdpReadytoSend += async (SdpMessage message) =>
            {
                await HubSignaling.Invoke("SendAnswer", mac.ID, message.Content);

            };

            pc.IceCandidateReadytoSend += async (IceCandidate candidate) =>
            {
                await HubSignaling.Invoke("SendIceCandidate", mac.ID, candidate);
            };

            /*Aggiunge Trasciver per trasmissione video*/
            videoSource = ExternalVideoTrackSource.CreateFromI420ACallback(CompleteRequest);
            
            var videoTrackConfig = new LocalVideoTrackInitConfig
            {
                trackName = "video_track"
            };
                
            localVideoTrack = LocalVideoTrack.CreateFromSource(videoSource, videoTrackConfig);
                

            var transcInitSett = new TransceiverInitSettings
            {
                Name = "Ricetrasmettitore_Schermo",
                InitialDesiredDirection = Transceiver.Direction.SendOnly
            };

            trans = pc.AddTransceiver(MediaKind.Video, transcInitSett);
            trans.LocalVideoTrack = localVideoTrack; //Agganciato trasmettitore con fonte video
        }


        //------------------------------TOOLS------------------------------------

        private void CompleteRequest(in FrameRequest request)
        {
            // 1. Cattura lo schermo (aggiorna l'array byte[])
            CaptureScreenBytes(schermo);

            // 2. Prepariamo i buffer per i piani I420
            byte[] yPlane = new byte[_lastWidth * _lastHeight];
            byte[] uPlane = new byte[(_lastWidth / 2) * (_lastHeight / 2)];
            byte[] vPlane = new byte[(_lastWidth / 2) * (_lastHeight / 2)];

            unsafe
            {
                fixed (byte* src = _lastArgb32Pixels)
                fixed (byte* yPtr = yPlane, uPtr = uPlane, vPtr = vPlane)
                {
                    for (int y = 0; y < _lastHeight; y++)
                    {
                        for (int x = 0; x < _lastWidth; x++)
                        {
                            // Individua il pixel ARGB (BGRA in memoria GDI+)
                            int i = (y * _lastArgb32Stride) + (x * 4);
                            byte b = src[i];
                            byte g = src[i + 1];
                            byte r = src[i + 2];

                            // Conversione in Piano Y (Luminosità)
                            yPtr[y * _lastWidth + x] = (byte)(((66 * r + 129 * g + 25 * b + 128) >> 8) + 16);

                            // Conversione in Piani U e V (Crominanza) ogni 2x2 pixel
                            if (x % 2 == 0 && y % 2 == 0)
                            {
                                int uvIdx = (y / 2) * (_lastWidth / 2) + (x / 2);
                                uPtr[uvIdx] = (byte)(((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128);
                                vPtr[uvIdx] = (byte)(((112 * r - 94 * g - 18 * b + 128) >> 8) + 128);
                            }
                        }
                    }

                    // 3. Compilazione della richiesta WebRTC
                    var i420Frame = new I420AVideoFrame
                    {
                        width = (uint)_lastWidth,
                        height = (uint)_lastHeight,
                        dataY = (IntPtr)yPtr,
                        dataU = (IntPtr)uPtr,
                        dataV = (IntPtr)vPtr,
                        strideY = _lastWidth,
                        strideU = _lastWidth / 2,
                        strideV = _lastWidth / 2
                    };

                    request.CompleteRequest(i420Frame);
                }
            }
        }

        /*Cattura le immagini del monitor indicato e le memorizza sia in _lastI420Frame (x trasmissione WebRTC) che in _lastArgb32Pixels (per trasmissione SignalR)*/
        private void CaptureScreenBytes(int monitorIndex)
        {
            var screen = Screen.AllScreens[monitorIndex];
            var bounds = screen.Bounds;

            // Gestione Bitmap e Graphics (riutilizzo)
            if (_captureBitmap == null || _monitorBounds != bounds)
            {
                _captureBitmap?.Dispose();
                _captureGraphics?.Dispose();
                _captureBitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppPArgb);
                _captureGraphics = Graphics.FromImage(_captureBitmap);
                _monitorBounds = bounds;
            }

            _captureGraphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);

            // Lock dei bit per estrarre i byte ARGB
            var data = _captureBitmap.LockBits(new Rectangle(0, 0, bounds.Width, bounds.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

            try
            {
                int size = data.Stride * bounds.Height;
                if (_lastArgb32Pixels == null || _lastArgb32Pixels.Length != size)
                    _lastArgb32Pixels = new byte[size];

                // Copia i pixel nel byte[]
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, _lastArgb32Pixels, 0, size);

                _lastWidth = bounds.Width;
                _lastHeight = bounds.Height;
                _lastArgb32Stride = data.Stride;
            }
            finally
            {
                _captureBitmap.UnlockBits(data);
            }
        }

        /*Timer per ricezione ed esecuzione comandi di mouse e tastiera ricevuti dal pc remoto*/
        private void StartMouseMoveCommandTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 4; // ~240 FPS
            _renderTimer.Tick += RenderMouseMove;
            _renderTimer.Start();
        }

        /*Timer per cattura schermo*/
        /*private void StartCaptureScreenBytesTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 66; // ~60 FPS
            _renderTimer.Tick += (e, s) => CaptureScreenBytes(schermo);
            _renderTimer.Start();
        }*/

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

        //-------------------------MOUSE E TASTIERA-------------------------------
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
