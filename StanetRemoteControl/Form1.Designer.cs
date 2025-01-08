using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Text.Json;
using System.Windows.Forms.VisualStyles;

namespace StanetRemoteControl
{
    partial class Form1
    {
        /// <summary>
        /// Variabile di progettazione necessaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Pulire le risorse in uso.
        /// </summary>
        /// <param name="disposing">ha valore true se le risorse gestite devono essere eliminate, false in caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Codice generato da Progettazione Windows Form

        /// <summary>
        /// Metodo necessario per il supporto della finestra di progettazione. Non modificare
        /// il contenuto del metodo con l'editor di codice.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Monotype Corsiva", 36F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(99, 79);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(238, 72);
            this.label1.TabIndex = 0;
            this.label1.Text = "Il tuo ID";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(114, 151);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(0, 38);
            this.label2.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 19.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(487, 151);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(0, 38);
            this.label3.TabIndex = 3;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Monotype Corsiva", 36F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(445, 79);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(242, 72);
            this.label4.TabIndex = 2;
            this.label4.Text = "Password";
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 30F);
            this.button1.Location = new System.Drawing.Point(257, 269);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(263, 77);
            this.button1.TabIndex = 4;
            this.button1.Text = "Connetti";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.Color.Red;
            this.label5.Location = new System.Drawing.Point(12, 424);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(0, 24);
            this.label5.TabIndex = 5;
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.WindowText;
            this.panel1.Location = new System.Drawing.Point(13, 13);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(775, 425);
            this.panel1.TabIndex = 6;
            this.panel1.Visible = false;

            /*Eventi*/
            this.panel1.Click += new EventHandler(panelClick);
            this.panel1.DoubleClick += new EventHandler(panelDoubleClick);
            this.panel1.PreviewKeyDown += new PreviewKeyDownEventHandler(panelKey);
            this.panel1.MouseClick += new MouseEventHandler(panelMouseClick);
            this.panel1.MouseDoubleClick += new MouseEventHandler(panelMouseDoubleClick);
            this.panel1.MouseDown += new MouseEventHandler(panelMouseDown);
            


            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        private void panelClick(object sender, EventArgs e) 
        {
            Comando = JsonSerializer.Serialize<EventArgs>(e);
        }

        private void panelDoubleClick(object sender, EventArgs e)
        {
            Comando = JsonSerializer.Serialize<EventArgs>(e);
        }

        private void panelKey(object sender, PreviewKeyDownEventArgs e) 
        {
            Comando = JsonSerializer.Serialize<PreviewKeyDownEventArgs>(e);
        }

        private void panelMouseClick(object sender, MouseEventArgs e)
        {
            Comando = JsonSerializer.Serialize<MouseEventArgs>(e);
        }

        private void panelMouseDoubleClick(object sender, MouseEventArgs e)
        {
            Comando = JsonSerializer.Serialize<MouseEventArgs>(e);
        }

        private void panelMouseDown(object sender, MouseEventArgs e)
        {
            Comando = JsonSerializer.Serialize<MouseEventArgs>(e);
        }



        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label5;

        public string MioId 
        {
            get { return label2.Text; }
            set { label2.Text = value; }
        }

        public string MiaPsw 
        {
            get { return label3.Text; }
            set { label3.Text = value; }
        }

        public string Errore 
        {
            get { return label5.Text; }
            set { label5.Text = value; }
        }

        public string Comando;
        private Panel panel1;
    }
}

