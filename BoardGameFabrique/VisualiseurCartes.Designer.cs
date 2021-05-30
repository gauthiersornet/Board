namespace BoardGameFabrique
{
    partial class VisualiseurCartes
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
            this.SuspendLayout();
            // 
            // VisualiseurCartes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1076, 899);
            this.DoubleBuffered = true;
            this.Name = "VisualiseurCartes";
            this.Text = "Visualiseur de cartes";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.VisualiseurCartes_FormClosing);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.VisualiseurCartes_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.VisualiseurCartes_KeyUp);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.VisualiseurCartes_MouseClick);
            this.ResumeLayout(false);

        }

        #endregion
    }
}