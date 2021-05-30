namespace BoardGameFabrique
{
    partial class BoardGameFabrique
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BoardGameFabrique));
            this.SuspendLayout();
            // 
            // BoardGameFabrique
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1720, 1055);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BoardGameFabrique";
            this.Text = "BoardGameFabrique";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.BoardGameFabrique_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.BoardGameFabrique_DragEnter);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.BoardGameFabrique_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.BoardGameFabrique_KeyUp);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.BoardGameFabrique_MouseClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.BoardGameFabrique_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.BoardGameFabrique_MouseMove);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

