using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StanetRemoteControl.RemoteControlNode;

namespace StanetRemoteControl
{
    partial class Rubrica
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoScroll = true;
            this.flowLayoutPanel1.AutoSize = false;
            this.flowLayoutPanel1.Size = this.flowLayoutPanel2.Size;
            this.flowLayoutPanel1.Margin = new Padding(5, 0, 0, 0);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = FlowDirection.TopDown;
            this.flowLayoutPanel1.WrapContents = false;
            this.flowLayoutPanel1.BackColor = Color.LightGray;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.TabIndex = 0;
            this.flowLayoutPanel1.Click += (s, e) => this.flowLayoutPanel1.Focus();
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoScroll = true;
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel2.FlowDirection = FlowDirection.TopDown;
            this.flowLayoutPanel2.WrapContents = false;
            this.flowLayoutPanel2.BackColor = Color.White;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel2.Name = "flowLayoutPanel1";
            this.flowLayoutPanel2.TabIndex = 0;
            this.flowLayoutPanel2.Click += (s, e) => this.flowLayoutPanel1.Focus();
            // 
            // Form Rubrica
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = Color.LightGray;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Width = this.flowLayoutPanel1.Width + 100;
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Name = "Rubrica";
            this.Text = "Rubrica";
            this.ResumeLayout(false);
        }



        #endregion

        public Timer animTimer;
        bool nascosta = false; // false -> fuori, true -> dentro
        int step = 5;
        int targetWhidth;
        IService1 service;
        Contatto[] rubrica;
        private FlowLayoutPanel flowLayoutPanel1;
        private FlowLayoutPanel flowLayoutPanel2;
        Int64 IdMacchina;
        private Controllore controllore;
        public TextBox ricerca = new TextBox();
    }
}