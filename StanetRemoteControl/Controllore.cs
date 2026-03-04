using Microsoft.AspNet.SignalR.Client;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
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
            return;
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

            // Ricezione screen
            HubSchermo.On<byte[]>("ReceiveScreen", screenBytes => {
                _latestFrame = screenBytes;
            });

            // riconnessione semplice a SignalR
            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            connection.Closed += async () =>
            {
                await Task.Delay(1000);
                try
                {
                    await connection.Start();
                    await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
                }
                catch { Console.WriteLine("Errore connessione"); }
            };

            await connection.Start();
            await HubSchermo.Invoke("JoinConnection", connUscita.ID_Controllato);
            await HubPreciousCommand.Invoke("JoinConnection", connUscita.ID_Controllato);
            await HubMouseMove.Invoke("JoinConnection", connUscita.ID_Controllato);
            StartSendMouseMoveCommandTimer();
            RenderFrameTimer();
        }

        public void button1_Click(string UserName, string Password)
        {

            connUscita = serv.ChiediAccesso(UserName, Password, IdMacchina);
            
            if (connUscita.ID != 0) 
            {
                textBox1.Size = new Size(0, 0);
                textBox2.Size = new Size(0, 0);
                button1.Size = new Size(0, 0);

                Width = Screen.PrimaryScreen.Bounds.Width * 60 / 100;
                Height = Screen.PrimaryScreen.Bounds.Height * 60 / 100;
                
                //proviamo cosi
                pictureBox1.Width = Width; pictureBox1.Height = Height;

                pictureBox1.Visible = true;

                //_ = AggiornaPictureBoxAsync();
                _ = InizializzaSignalR();
            }
        }

        private void StartSendMouseMoveCommandTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 100; // ~10 FPS
            _renderTimer.Tick += SendMouseMoveCommand;
            _renderTimer.Start();
        }

        private void RenderFrameTimer()
        {
            _renderTimer = new System.Windows.Forms.Timer();
            _renderTimer.Interval = 1; // ~10 FPS
            _renderTimer.Tick += RenderFrame;
            _renderTimer.Start();
        }

        private void RenderFrame(object sender, EventArgs e)
        {
            if (_latestFrame == null || _latestFrame.Length <= 16)
                return;

            Console.WriteLine((DateTime.UtcNow.Ticks - BitConverter.ToInt64(_latestFrame, 0)) / 10000000.0);

            if ((DateTime.UtcNow.Ticks - BitConverter.ToInt64(_latestFrame, 0)) / 10000000.0 > 1.5) //10000 ticks = 1 ms
            {
                return;
            } 

            int width = BitConverter.ToInt32(_latestFrame, 8);
            int height = BitConverter.ToInt32(_latestFrame, 12);

            using (var ms = new MemoryStream(_latestFrame, 16, _latestFrame.Length - 16, false))
            {
                var thumb = new Bitmap(ms); // thumbnail ricevuto dal controllato

                // Aggiorna PictureBox con Zoom
                if (pictureBox1.InvokeRequired)
                {
                    pictureBox1.BeginInvoke(new Action(() =>
                    {
                        var old = pictureBox1.Image;
                        pictureBox1.Image = thumb;
                        old?.Dispose();
                    }));
                }
                else
                {
                    var old = pictureBox1.Image;
                    pictureBox1.Image = thumb;
                    old?.Dispose();
                }
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
