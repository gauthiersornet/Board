namespace BoardGameFabrique
{
    partial class Décomposer
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
            this.btValid = new System.Windows.Forms.Button();
            this.txtNbCol = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtNbLig = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtNbCartes = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btValid
            // 
            this.btValid.Location = new System.Drawing.Point(105, 118);
            this.btValid.Name = "btValid";
            this.btValid.Size = new System.Drawing.Size(72, 30);
            this.btValid.TabIndex = 3;
            this.btValid.Text = "Valider";
            this.btValid.UseVisualStyleBackColor = true;
            this.btValid.Click += new System.EventHandler(this.btValid_Click);
            // 
            // txtNbCol
            // 
            this.txtNbCol.Location = new System.Drawing.Point(169, 6);
            this.txtNbCol.Name = "txtNbCol";
            this.txtNbCol.Size = new System.Drawing.Size(111, 22);
            this.txtNbCol.TabIndex = 0;
            this.txtNbCol.Text = "1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(151, 17);
            this.label1.TabIndex = 2;
            this.label1.Text = "Nombre de colonnes : ";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(131, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "Nombre de lignes : ";
            // 
            // txtNbLig
            // 
            this.txtNbLig.Location = new System.Drawing.Point(169, 39);
            this.txtNbLig.Name = "txtNbLig";
            this.txtNbLig.Size = new System.Drawing.Size(111, 22);
            this.txtNbLig.TabIndex = 1;
            this.txtNbLig.Text = "1";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 71);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(133, 17);
            this.label3.TabIndex = 5;
            this.label3.Text = "Nombre de cartes : ";
            // 
            // txtNbCartes
            // 
            this.txtNbCartes.Location = new System.Drawing.Point(169, 71);
            this.txtNbCartes.Name = "txtNbCartes";
            this.txtNbCartes.Size = new System.Drawing.Size(111, 22);
            this.txtNbCartes.TabIndex = 2;
            this.txtNbCartes.Text = "1";
            // 
            // Décomposer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 156);
            this.Controls.Add(this.txtNbCartes);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtNbLig);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtNbCol);
            this.Controls.Add(this.btValid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Décomposer";
            this.Text = "Décomposer";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btValid;
        private System.Windows.Forms.TextBox txtNbCol;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtNbLig;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtNbCartes;
    }
}