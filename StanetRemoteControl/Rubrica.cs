using NPOI.SS.Formula.Functions;
using StanetRemoteControl.RemoteControlNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace StanetRemoteControl
{
    public partial class Rubrica : Form
    {
        public Rubrica(Controllore Controllore, IService1 serv, Int64 IdMac)
        {
            InitializeComponent();
            controllore = Controllore;
            Owner = Controllore;
            animTimer = new Timer();
            animTimer.Interval = 15; // più piccolo = animazione più fluida
            animTimer.Tick += AnimTimer_Tick;
            service = serv;
            IdMacchina = IdMac;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.TopMost = false;

            // Parte fuori
            this.Left = Owner.Right;
            this.Top = Owner.Top;
            this.Height = Owner.Height;



            LoadButtons();
            //AdattaLarghezzaForm();
            targetWhidth = this.Width;
            ArrotondaAngoli(15);

            this.Show(); // sempre visibile
        }

        private void AnimTimer_Tick(object sender, EventArgs e)
        {
            if (nascosta) // se nascosta = true, allora voglio aprire
            {
                this.Visible = true;
                this.Width += step;
                ArrotondaAngoli(15);

                if (this.Width >= targetWhidth)
                {
                    this.Width = targetWhidth;
                    nascosta = false; // ora è visibile

                    animTimer.Stop();
                }
            }
            else // se visibile, voglio chiudere
            {
                this.Width -= step;
                ArrotondaAngoli(15);

                if (this.Width == 2)
                {
                    this.Width = 0;
                    nascosta = true; // ora è nascosta
                    this.Visible = false;
                    animTimer.Stop();
                }
            }
        }


        public void SyncPosition()
        {
            if (Owner == null) return;

            this.Top = Owner.Top;
            this.Height = Owner.Height;
            this.Left = Owner.Right;

            ArrotondaAngoli(15);
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

        private void LoadButtons()
        {
            flowLayoutPanel1.Controls.Clear();
            
            Label l = new Label();
            l.Text = "Rubrica";
            l.AutoSize = true;
            l.Dock = DockStyle.Top;
            l.TextAlign = ContentAlignment.MiddleLeft;
            l.Font = new Font("Segoe UI", 18f, FontStyle.Bold);
            l.ForeColor = SystemColors.WindowText;
            l.BackColor = SystemColors.Control;
            l.Padding = new Padding(0, 2, 0, 0);
            l.Margin = new Padding(0, 0, 15, 0);
            flowLayoutPanel1.Controls.Add(l);

            ricerca.Text = "";
            ricerca.KeyUp += (s, e) => { flowLayoutPanel2.Controls.Clear(); CaricaContatti(ricerca.Text); flowLayoutPanel1.Controls.Add(flowLayoutPanel2); };
            flowLayoutPanel1.Controls.Add(ricerca);

            flowLayoutPanel2.Controls.Clear();
            rubrica = service.GetRubrica(IdMacchina);
            CaricaContatti(ricerca.Text);
            flowLayoutPanel1.Controls.Add(flowLayoutPanel2);
        }

        private void CaricaContatti(string filtro)
        {
            

            var gruppi = rubrica
                .GroupBy(c => (c.IdGruppo, c.NomeGruppo))
                .ToList();

            foreach (var gruppo in gruppi)
            {
                if ((gruppo.Key.NomeGruppo).Contains(filtro) || (gruppo.Key.NomeGruppo.ToLower()).Contains(filtro) || (gruppo.Key.NomeGruppo).Contains(filtro.ToLower()) || (gruppo.Key.NomeGruppo.ToLower()).Contains(filtro.ToLower()))
                {
                    /*Gruppo*/
                    FlowLayoutPanel flowLayoutPanelGruppo = new FlowLayoutPanel();
                    flowLayoutPanelGruppo.FlowDirection = FlowDirection.LeftToRight;
                    flowLayoutPanelGruppo.AutoSize = true;
                    flowLayoutPanel2.Controls.Add(flowLayoutPanelGruppo);

                    Label lbl = new Label();
                    lbl.Text = gruppo.Key.NomeGruppo;
                    lbl.Font = new Font(lbl.Font, FontStyle.Bold);
                    lbl.AutoSize = true;
                    lbl.Margin = new Padding(5, 7, 0, 0);

                    // TextBox per rinomina
                    TextBox newNameGruppo = new TextBox();
                    newNameGruppo.Visible = false;
                    newNameGruppo.Size = lbl.Size;
                    newNameGruppo.KeyUp += (s, e) =>
                    {
                        if (e.KeyCode == Keys.Enter && newNameGruppo.Text.CompareTo("") != 0)
                        {
                            service.RinominaGruppo(gruppo.Key.IdGruppo, newNameGruppo.Text);
                            rubrica = service.GetRubrica(IdMacchina);
                            flowLayoutPanel2.Controls.Clear();
                            CaricaContatti(ricerca.Text);
                        }

                    };
                    newNameGruppo.LostFocus += (s, e) => { 
                        if (newNameGruppo.Text.CompareTo("") != 0)
                        {
                            service.RinominaGruppo(gruppo.Key.IdGruppo, newNameGruppo.Text);
                            rubrica = service.GetRubrica(IdMacchina);
                            flowLayoutPanel2.Controls.Clear();
                            CaricaContatti(ricerca.Text);
                        }
                    };
                    flowLayoutPanelGruppo.Controls.Add(newNameGruppo);

                    // Menu contestuale
                    ContextMenuStrip menuGruppo = new ContextMenuStrip();
                    menuGruppo.Items.Add("Aggiungi Contatto", null, (s, e) => { 
                        AggiungiContattoDaRubrica(gruppo.Key.IdGruppo); 
                        rubrica = service.GetRubrica(IdMacchina);
                        flowLayoutPanel2.Controls.Clear();
                        CaricaContatti(ricerca.Text);
                    });
                    menuGruppo.Items.Add("Rinomina", null, (s, e) => { newNameGruppo.Text = lbl.Text; lbl.Visible = false; newNameGruppo.Visible = true; newNameGruppo.Focus(); });
                    menuGruppo.Items.Add("Elimina", null, (s, e) => CancellaGruppo(gruppo.Key.IdGruppo, gruppo.Key.NomeGruppo));

                    // Button per aprire il menu
                    Button apriMenuGruppo = new Button();
                    apriMenuGruppo.Text = "⋮";
                    apriMenuGruppo.AutoSize = true;
                    apriMenuGruppo.Click += (s, e) => menuGruppo.Show(apriMenuGruppo, new Point(0, apriMenuGruppo.Height));
                    

                    /*eventi label gruppo*/
                    lbl.MouseDoubleClick += (s, e) => { if (e.Button == MouseButtons.Left) { newNameGruppo.Text = lbl.Text; lbl.Visible = false; newNameGruppo.Visible = true; newNameGruppo.Focus(); } };
                    flowLayoutPanelGruppo.Controls.Add(lbl);
                    flowLayoutPanelGruppo.Controls.Add(apriMenuGruppo);


                    foreach (var contatto in gruppo)
                    {
                        // Panel/FlowLayoutPanel per riga orizzontale
                        FlowLayoutPanel row = new FlowLayoutPanel();
                        row.FlowDirection = FlowDirection.LeftToRight;
                        row.WrapContents = false;
                        row.AutoSize = true;
                        row.Margin = new Padding(5, 5, 0, 5);

                        // Button principale
                        Button btn = new Button();
                        btn.Text = contatto.Nome;
                        btn.Tag = contatto;
                        btn.AutoSize = true;
                        btn.Click += (s, e) => ConnessioneDaRubrica(contatto.UserName, contatto.Password);
                        row.Controls.Add(btn);

                        // TextBox per rinomina
                        TextBox newNameContatto = new TextBox();
                        newNameContatto.Visible = false;
                        newNameContatto.Size = btn.Size;
                        newNameContatto.KeyUp += (s, e) => { if (e.KeyCode == Keys.Enter && newNameContatto.Text.CompareTo("") != 0) { service.RinominaContattoInGruppo(((Contatto)btn.Tag).UserName, ((Contatto)btn.Tag).IdGruppo, newNameContatto.Text); rubrica = service.GetRubrica(IdMacchina); flowLayoutPanel2.Controls.Clear(); CaricaContatti(ricerca.Text); } };
                        newNameContatto.LostFocus += (s, e) => { if (newNameContatto.Text.CompareTo("") != 0) { service.RinominaContattoInGruppo(((Contatto)btn.Tag).UserName, ((Contatto)btn.Tag).IdGruppo, newNameContatto.Text); rubrica = service.GetRubrica(IdMacchina); flowLayoutPanel2.Controls.Clear(); CaricaContatti(ricerca.Text); } };
                        row.Controls.Add(newNameContatto);

                        // Menu contestuale
                        ContextMenuStrip menu = new ContextMenuStrip();
                        menu.Items.Add("Rinomina", null, (s, e) => { newNameContatto.Text = btn.Text; btn.Visible = false; newNameContatto.Visible = true; newNameContatto.Focus(); });
                        menu.Items.Add("Elimina", null, (s, e) => CancellaContatto((Contatto)btn.Tag));

                        // Button per aprire il menu
                        Button apriMenu = new Button();
                        apriMenu.Text = "⋮";
                        apriMenu.AutoSize = true;
                        apriMenu.Click += (s, e) => menu.Show(apriMenu, new Point(0, apriMenu.Height));
                        row.Controls.Add(apriMenu);

                        // Aggiungo la riga al flowLayoutPanel principale
                        flowLayoutPanel2.Controls.Add(row);
                    }
                }
                else 
                {
                    foreach (var contatto in gruppo)
                    {
                        if ((contatto.Nome).Contains(filtro) || (contatto.Nome.ToLower()).Contains(filtro) || (contatto.Nome).Contains(filtro.ToLower()) || (contatto.Nome.ToLower()).Contains(filtro.ToLower()))
                        {
                            // Panel/FlowLayoutPanel per riga orizzontale (macchina e impostazioni)
                            FlowLayoutPanel rowGenerale = new FlowLayoutPanel();
                            rowGenerale.FlowDirection = FlowDirection.TopDown;
                            rowGenerale.WrapContents = false;
                            rowGenerale.AutoSize = true;
                            rowGenerale.Margin = new Padding(5, 5, 0, 5);

                            // Panel/FlowLayoutPanel per riga orizzontale (macchina e impostazioni)
                            FlowLayoutPanel rowdaGruppo = new FlowLayoutPanel();
                            rowdaGruppo.WrapContents = false;
                            rowdaGruppo.AutoSize = true;
                            rowdaGruppo.Margin = new Padding(5, 5, 0, 5);
                            rowGenerale.Controls.Add(rowdaGruppo);

                            // Panel/FlowLayoutPanel per riga orizzontale (macchina e impostazioni)
                            FlowLayoutPanel row = new FlowLayoutPanel();
                            row.FlowDirection = FlowDirection.LeftToRight;
                            row.WrapContents = false;
                            row.AutoSize = true;
                            row.Margin = new Padding(5, 5, 0, 5);
                            rowGenerale.Controls.Add(row);

                            Label daGruppo = new Label();
                            daGruppo.Text = contatto.NomeGruppo;
                            daGruppo.AutoSize = true;
                            daGruppo.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                            daGruppo.ForeColor = Color.FromArgb(140, 140, 140); // grigino moderno
                            daGruppo.Margin = new Padding(0, 2, 0, 0);
                            rowdaGruppo.Controls.Add(daGruppo);

                            // Button principale
                            Button btn = new Button();
                            btn.Text = contatto.Nome;
                            btn.Tag = contatto;
                            btn.AutoSize = true;
                            btn.Click += (s, e) => ConnessioneDaRubrica(contatto.UserName, contatto.Password);
                            row.Controls.Add(btn);

                            // TextBox per rinomina
                            TextBox newNameContatto = new TextBox();
                            newNameContatto.Visible = false;
                            newNameContatto.Size = btn.Size;
                            newNameContatto.KeyUp += (s, e) => { if (e.KeyCode == Keys.Enter && newNameContatto.Text.CompareTo("") != 0) { service.RinominaContattoInGruppo(((Contatto)btn.Tag).UserName, ((Contatto)btn.Tag).IdGruppo, newNameContatto.Text); rubrica = service.GetRubrica(IdMacchina); flowLayoutPanel2.Controls.Clear(); CaricaContatti(ricerca.Text); } };
                            newNameContatto.LostFocus += (s, e) => { if (newNameContatto.Text.CompareTo("") != 0) { service.RinominaContattoInGruppo(((Contatto)btn.Tag).UserName, ((Contatto)btn.Tag).IdGruppo, newNameContatto.Text); rubrica = service.GetRubrica(IdMacchina); flowLayoutPanel2.Controls.Clear(); CaricaContatti(ricerca.Text); } };
                            row.Controls.Add(newNameContatto);

                            // Menu contestuale
                            ContextMenuStrip menu = new ContextMenuStrip();
                            menu.Items.Add("Rinomina", null, (s, e) => { newNameContatto.Text = btn.Text; btn.Visible = false; newNameContatto.Visible = true; newNameContatto.Focus(); });
                            menu.Items.Add("Elimina", null, (s, e) => CancellaContatto((Contatto)btn.Tag));

                            // Button per aprire il menu
                            Button apriMenu = new Button();
                            apriMenu.Text = "⋮";
                            apriMenu.AutoSize = true;
                            apriMenu.Click += (s, e) => menu.Show(apriMenu, new Point(0, apriMenu.Height));
                            row.Controls.Add(apriMenu);

                            // Aggiungo la riga al flowLayoutPanel principale
                            flowLayoutPanel2.Controls.Add(rowGenerale);
                        }
                    }
                }
            }
        }

        private void Btn_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            var item = btn.Tag as Contatto;

            MessageBox.Show(item.UserName);
        }

        /*private void AdattaLarghezzaForm()
        {
            int maxWidth = 0;

            foreach (Control ctrl in flowLayoutPanel1.Controls)
            {
                maxWidth = Math.Max(maxWidth, ctrl.Right);
            }

            foreach (Control ctrl in flowLayoutPanel2.Controls)
            {
                maxWidth = Math.Max(maxWidth, ctrl.Right);
            }


            // padding + bordi
            this.Width = maxWidth + 20;
            flowLayoutPanel1.AutoSize = true;
            flowLayoutPanel2.AutoSize = true;
        }*/

        private void CancellaContatto(Contatto c) 
        {
            DialogResult result = MessageBox.Show(
            "Sei sicuro di voler cancellare " + c.Nome + " dalla tua rubrica?",
            "Conferma",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question
            );

            if (result == DialogResult.OK)
            {
                service.RimuoviContattoDaGruppo(c.UserName, c.IdGruppo);
            }

            rubrica = service.GetRubrica(IdMacchina);
            flowLayoutPanel2.Controls.Clear();
            CaricaContatti(ricerca.Text);
        }

        private void CancellaGruppo(Int64 IdGruppo, string NomeGruppo)
        {
            DialogResult result = MessageBox.Show(
            "Sei sicuro di voler cancellare " + NomeGruppo + " dalla tua rubrica?\nTutti i contatti di questo gruppo saranno cancellati.",
            "Conferma",
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question
            );

            if (result == DialogResult.OK)
            {
                service.RimuoviGruppo(IdGruppo);
            }

            rubrica = service.GetRubrica(IdMacchina);
            flowLayoutPanel2.Controls.Clear();
            CaricaContatti(ricerca.Text);
        }

        private void AggiungiContattoDaRubrica(Int64 IdGruppo)
        {
            var nuovoContatto = new Contatto();
            var dialogResult = new DialogResult();
            using (var dlg = new ContattoDialog())
            {
                do
                {
                    dialogResult = dlg.ShowDialog();
                    if (dialogResult == DialogResult.OK)
                    {
                        string nome = dlg.Nominativo;
                        string user = dlg.Username;
                        string pass = dlg.Password;

                        nuovoContatto = service.AggiungiContattoAGruppo(user, pass, IdGruppo, nome);
                        if (nuovoContatto.Nome.CompareTo("") == 0)
                        {
                            dialogResult = MessageBox.Show(
                                nuovoContatto.UserName, //ci ho salvato l'errore
                                "Errore",
                                MessageBoxButtons.RetryCancel,
                                MessageBoxIcon.Error);
                        }
                        else 
                        {
                            dialogResult = MessageBox.Show(
                                "Contatto inserito correttamente",
                                "Conferma",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }

                            
                    }
                }while(!(nuovoContatto.Nome.CompareTo("") != 0 || dialogResult == DialogResult.Cancel));
            }

        }

        private void ConnessioneDaRubrica(string UserName, string Password) 
        {
            if (controllore.connUscita.ID_Controllato != 0) 
            {
                controllore.HubSchermo.Invoke("LeaveConnection", controllore.connUscita.ID_Controllato);
                controllore.HubMouseMove.Invoke("LeaveConnection", controllore.connUscita.ID_Controllato);
                controllore.HubPreciousCommand.Invoke("LeaveConnection", controllore.connUscita.ID_Controllato);
            }

            controllore.button1_Click(UserName, Password);
        }

    }

    public class ContattoDialog : Form
    {
        TextBox txtNominativo;
        TextBox txtUsername;
        TextBox txtPassword;
        CheckBox chkMostra;
        Button btnInvio;
        Button btnCancella;

        public string Nominativo => txtNominativo.Text;
        public string Username => txtUsername.Text;
        public string Password => txtPassword.Text;

        public ContattoDialog()
        {
            // ===== Form stile MessageBox =====
            this.Text = "Nuovo contatto";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(360, 230);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.Icon = SystemIcons.Information;

            // ===== Icona =====
            var icon = new PictureBox
            {
                Image = SystemIcons.Information.ToBitmap(),
                Location = new Point(15, 20),
                Size = new Size(32, 32)
            };
            this.Controls.Add(icon);

            // ===== Testo =====
            var lbl = new Label
            {
                Text = "Inserisci i dati del contatto",
                Location = new Point(60, 25),
                AutoSize = true
            };
            this.Controls.Add(lbl);

            // ===== TextBox =====
            txtNominativo = CreaTextBox(60, 60, "Nome e Cognome");
            txtUsername = CreaTextBox(60, 95, "Username");
            txtPassword = CreaTextBox(60, 130, "Password");
            txtPassword.UseSystemPasswordChar = true;

            this.Controls.AddRange(new Control[]
            {
            txtNominativo, txtUsername, txtPassword
            });

            // ===== Mostra Password =====
            chkMostra = new CheckBox
            {
                Text = "Mostra password",
                Location = new Point(60, 160),
                AutoSize = true
            };
            chkMostra.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkMostra.Checked;
            };
            this.Controls.Add(chkMostra);

            // ===== Pulsanti =====
            btnInvio = new Button
            {
                Text = "Invio",
                Location = new Point(185, 190),
                Width = 75,
                DialogResult = DialogResult.OK
            };

            btnCancella = new Button
            {
                Text = "Cancella",
                Location = new Point(265, 190),
                Width = 75,
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { btnInvio, btnCancella });

            this.AcceptButton = btnInvio;
            this.CancelButton = btnCancella;
        }

        // ===== Placeholder Win32 =====
        const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern Int32 SendMessage(
            IntPtr hWnd, int msg, int wParam, string lParam);

        TextBox CreaTextBox(int x, int y, string placeholder)
        {
            var tb = new TextBox
            {
                Location = new Point(x, y),
                Width = 260
            };
            SendMessage(tb.Handle, EM_SETCUEBANNER, 0, placeholder);
            return tb;
        }
    }
}
