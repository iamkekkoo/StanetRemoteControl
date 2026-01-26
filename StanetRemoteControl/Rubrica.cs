using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace StanetRemoteControl
{
    public partial class Rubrica : Form
    {
        public Rubrica(Form Controllore)
        {
            InitializeComponent();
            Owner = Controllore;
            Width = 100;
            animTimer = new Timer();
            animTimer.Interval = 15; // più piccolo = animazione più fluida
            animTimer.Tick += AnimTimer_Tick;

            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = false;

            // Parte fuori
            this.Left = Owner.Right;
            this.Top = Owner.Top;
            this.Height = Owner.Height;
            

            this.Show(); // sempre visibile
        }

        public void SyncSize()
        {
            if (Owner == null) return;

            Top = Owner.Top;
            Height = Owner.Height - Owner.Height;        
        }

        public void Toggle()
        {
            if (!animTimer.Enabled)
            {
                if (nascosta)
                    targetLeft = Owner.Right - this.Width; // entra
                else
                    targetLeft = Owner.Right; // esce

                animTimer.Start();
            }
            ArrotondaAngoli(10);
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (nascosta)
            {
                this.Left -= step;
                if (this.Left <= targetLeft)
                {
                    this.Left = targetLeft;
                    nascosta = false;
                    animTimer.Stop();
                }
            }
            else
            {
                this.Left += step;
                if (this.Left >= targetLeft)
                {
                    this.Left = targetLeft;
                    nascosta = true;
                    animTimer.Stop();
                }
            }
        }

        public void SyncPosition()
        {
            this.Top = Owner.Top;
            this.Height = Owner.Height;

            if (nascosta)
                this.Left = Owner.Right;
            else
                this.Left = Owner.Right - this.Width + 10;

            ArrotondaAngoli(10);
        }

        private void ArrotondaAngoli(int raggio)
        {
            GraphicsPath path = new GraphicsPath();
            path.StartFigure();
            path.AddArc(new Rectangle(0, 0, raggio * 2, raggio * 2), 180, 90);                     // angolo in alto a sinistra
            path.AddArc(new Rectangle(this.Width - raggio * 2, 0, raggio * 2, raggio * 2), 270, 90); // angolo in alto a destra
            path.AddArc(new Rectangle(this.Width - raggio * 2, this.Height - raggio * 2, raggio * 2, raggio * 2), 0, 90); // basso destra
            path.AddArc(new Rectangle(0, this.Height - raggio * 2, raggio * 2, raggio * 2), 90, 90); // basso sinistra
            path.CloseFigure();

            this.Region = new Region(path);
        }
    }
}
