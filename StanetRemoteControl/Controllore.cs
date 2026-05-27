using Microsoft.AspNet.SignalR.Client;
using Microsoft.MixedReality.WebRTC;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.ServiceModel.Channels;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static NPOI.POIFS.Crypt.CryptoFunctions;


namespace StanetRemoteControl
{
    public partial class Controllore : Form
    {
        public Controllore()
        {
            InitializeComponent();
            this.pictureBox1.Visible = false;
            this.Load += Controllore_Load;
            this.Shown += Controllore_Shown;

            return;
        }
        private void Controllore_Load(object sender, EventArgs e) 
        {
            cts = new CancellationTokenSource();

            /*Inizializza variabili per signaling UDP*/
            

            return;
        }

        private void RemoteVideoTrack_I420AVideoFrameReady(I420AVideoFrame frame)
        {
            RenderFrame(frame);
            //throw new NotImplementedException();
        }

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

            pc.LocalSdpReadytoSend += async (SdpMessage message) =>
            {
                if (message.Type == SdpMessageType.Offer)
                {
                    await HubSignaling.Invoke("SendOffer", connUscita.ID_Controllato, message.Content);
                }
            };

            pc.IceCandidateReadytoSend += async (IceCandidate candidate) =>
            {
                await HubSignaling.Invoke("SendIceCandidate", connUscita.ID_Controllato, candidate);
                Console.WriteLine("Ice candidate inviato da controllore");

            };

            pc.VideoTrackAdded += (RemoteVideoTrack track) =>
            {
                Console.WriteLine("Ponte video stabilito! La traccia è pronta.");

                // Qui la traccia è garantita, non serve loop o delay
                track.I420AVideoFrameReady += RemoteVideoTrack_I420AVideoFrameReady;
            };


            // 1. Il ricevitore crea il suo transceiver per "ascoltare"
            var transcInitSett = new TransceiverInitSettings
            {
                Name = "Ricezione_Video",
                InitialDesiredDirection = Transceiver.Direction.ReceiveOnly // "Voglio solo ricevere"
            };
            pc.AddTransceiver(MediaKind.Video, transcInitSett);
        }

        private void Controllore_Shown(object sender, EventArgs e)
        {
            Rubrica = new Rubrica(this, serv, IdMacchina);
        }

        private async Task InizializzaSignalR()
        {
            connection = new HubConnection("https://francescostdev.azurewebsites.net");
            HubSchermo = connection.CreateHubProxy("ScreenHub");
            HubPreciousCommand = connection.CreateHubProxy("PreciousCommandHub");
            HubMouseMove = connection.CreateHubProxy("MouseMoveHub");
            HubSignaling = connection.CreateHubProxy("SignalingHub");

            // Hub ON
            { 
               HubSignaling.On<string, string>("ReceiveAnswer", async (connId, sdp) =>
                {
                    Console.WriteLine("Answer ricevuta da controllore");

                    if (!connesso) 
                    {
                        connesso = true;
                        var msg = new SdpMessage
                        {
                            Type = SdpMessageType.Answer,
                            Content = sdp
                        };
                        await pc.SetRemoteDescriptionAsync(msg);
                        Console.WriteLine("SDP: " + sdp);
                        
                    }
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
                    await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
                    await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
                    await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
                    await HubSignaling.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            await connection.Start();
            await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
            await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
            await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
            await HubSignaling.Invoke("JoinConnection", connUscita.ID_Controllato);
            

            /*Streaming TCP*/
            //StartSendMouseMoveCommandTimer();
            //RenderFrameTimer();
        }

        public async void button1_Click(string UserName, string Password)
        {

            connUscita = serv.ChiediAccesso(UserName, Password, IdMacchina);
            
            if (connUscita.ID != 0) //Connesso signalR
            {
                textBox1.Size = new Size(0, 0);
                textBox2.Size = new Size(0, 0);
                button1.Size = new Size(0, 0);

                Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;
                
                //proviamo cosi
                pictureBox1.Width = Width; pictureBox1.Height = Height;

                pictureBox1.Visible = true;

                _ = InitWebRTC();
                _ = InizializzaSignalR();
                await Task.Delay(5000);

                sendOffer();
            }
        }
        private async void sendOffer() { while (!connesso) { pc.CreateOffer(); Console.WriteLine("offer inviata"); await Task.Delay(5000); } }
        
        private void StartSendMouseMoveCommandTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 100; // ~10 FPS
            _renderTimer.Tick += SendMouseMoveCommand;
            _renderTimer.Start();
        }

        /*private void RenderFrameTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 1; // ~10 FPS
            _renderTimer.Tick += RenderFrame;
            _renderTimer.Start();
        }*/

        private void RenderFrame(I420AVideoFrame frame)
        {
            try
            {
                int w = (int)frame.width;
                int h = (int)frame.height;

                // 1. Gestione dinamica del canvas: lo ricreiamo solo se cambia la risoluzione
                if (_renderCanvas == null || _renderCanvas.Width != w || _renderCanvas.Height != h)
                {
                    _renderCanvas?.Dispose();
                    _renderCanvas = new Bitmap(w, h, PixelFormat.Format32bppArgb);
                }

                // 2. Accesso diretto alla memoria della Bitmap
                var data = _renderCanvas.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                unsafe
                {
                    byte* pDest = (byte*)data.Scan0;
                    byte* srcY = (byte*)frame.dataY;
                    byte* srcU = (byte*)frame.dataU;
                    byte* srcV = (byte*)frame.dataV;

                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            // Calcolo indici con Stride (fondamentale per WebRTC)
                            int yIdx = (y * frame.strideY) + x;
                            int uvIdx = ((y / 2) * frame.strideU) + (x / 2);

                            // Estrazione YUV
                            int Y = srcY[yIdx] - 16;
                            int U = srcU[uvIdx] - 128;
                            int V = srcV[uvIdx] - 128;

                            // Conversione YUV -> RGB (Formula Integer-Shift veloce)
                            int r = (298 * Y + 409 * V + 128) >> 8;
                            int g = (298 * Y - 100 * U - 208 * V + 128) >> 8;
                            int b = (298 * Y + 516 * U + 128) >> 8;

                            // Scrittura pixel in formato BGRA (con clamping manuale 0-255)
                            int dIdx = (y * data.Stride) + (x * 4);
                            pDest[dIdx] = (byte)(b < 0 ? 0 : (b > 255 ? 255 : b)); // Blue
                            pDest[dIdx + 1] = (byte)(g < 0 ? 0 : (g > 255 ? 255 : g)); // Green
                            pDest[dIdx + 2] = (byte)(r < 0 ? 0 : (r > 255 ? 255 : r)); // Red
                            pDest[dIdx + 3] = 255;                                     // Alpha
                        }
                    }
                }

                _renderCanvas.UnlockBits(data);

                // 3. Aggiornamento UI (Invoke necessario perché WebRTC lavora su un altro thread)
                pictureBox1.BeginInvoke(new Action(() =>
                {
                    var oldImg = pictureBox1.Image;
                    pictureBox1.Image = new Bitmap(_renderCanvas); // Copia per la UI
                    oldImg?.Dispose(); // Libera la memoria della vecchia immagine
                }));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel rendering: {ex.Message}");
            }
        }

        private void SendMouseMoveCommand(object sender, EventArgs e)
        {
            if (_LastMouseMoveCommandToSend == null)
                return;

            if (_LastMouseMoveCommandToSend != _LastMouseMoveCommandSent) 
            { 
                HubPreciousCommand.Invoke("SendCommand", connUscita.ID_Controllato, _LastMouseMoveCommandToSend);
                _LastMouseMoveCommandSent = _LastMouseMoveCommandToSend;
            }
            
        }

        public void ControlloreReset() 
        {
            // 
            // textBox1
            // 
            this.textBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBox1.Location = new System.Drawing.Point(60, 77);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(189, 44);
            this.textBox1.TabIndex = 0;
            // 
            // textBox2
            // 
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.Font = new System.Drawing.Font("Microsoft Sans Serif", 24F);
            this.textBox2.Location = new System.Drawing.Point(348, 77);
            this.textBox2.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(189, 44);
            this.textBox2.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(528, 329);
            this.button1.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(64, 27);
            this.button1.TabIndex = 2;
            this.button1.Text = "Invio";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // rubrica
            // 
            this.rubrica.Location = new System.Drawing.Point(this.Top, this.Right);
            this.rubrica.Name = "rubrica";
            this.rubrica.TabIndex = 2;
            this.rubrica.Text = "Rubrica";
            this.rubrica.UseVisualStyleBackColor = true;
            this.rubrica.Click += new System.EventHandler(this.btnToggleRubrica_Click);
            // 
            // Error
            // 
            this.Error.AutoSize = true;
            this.Error.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.Error.ForeColor = System.Drawing.Color.Red;
            this.Error.Location = new System.Drawing.Point(9, 344);
            this.Error.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Error.Name = "Error";
            this.Error.Size = new System.Drawing.Size(0, 13);
            this.Error.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.Color.Black;
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.pictureBox1.Visible = false;
            // 
            // Controllore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(600, 365);
            this.Margin = new System.Windows.Forms.Padding(2, 3, 2, 3);
        }
        

    }
}
