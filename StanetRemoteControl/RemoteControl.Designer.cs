namespace StanetRemoteControl
{
    partial class RemoteControl
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Schermo = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.Schermo)).BeginInit();
            this.SuspendLayout();
            // 
            // Schermo
            // 
            this.Schermo.Location = new System.Drawing.Point(13, 13);
            this.Schermo.Name = "Schermo";
            this.Schermo.Size = new System.Drawing.Size(775, 425);
            this.Schermo.TabIndex = 0;
            this.Schermo.TabStop = false;
            // 
            // RemoteControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Schermo);
            this.Name = "RemoteControl";
            this.Text = "RemoteControl";
            ((System.ComponentModel.ISupportInitialize)(this.Schermo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox Schermo;
    }
}