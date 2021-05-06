namespace Board
{
    partial class CréerSession
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
            this.chkBxDemanderMaitre = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.txtMdpMaitreConfirm = new System.Windows.Forms.TextBox();
            this.txtMdpJoueur = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.txtMdpMaitre = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtNomSession = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btCréer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // chkBxDemanderMaitre
            // 
            this.chkBxDemanderMaitre.AutoSize = true;
            this.chkBxDemanderMaitre.Checked = true;
            this.chkBxDemanderMaitre.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBxDemanderMaitre.Location = new System.Drawing.Point(15, 120);
            this.chkBxDemanderMaitre.Name = "chkBxDemanderMaitre";
            this.chkBxDemanderMaitre.Size = new System.Drawing.Size(282, 21);
            this.chkBxDemanderMaitre.TabIndex = 42;
            this.chkBxDemanderMaitre.Text = "demander l\'accord au maître de session";
            this.chkBxDemanderMaitre.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 63);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(124, 17);
            this.label8.TabIndex = 41;
            this.label8.Text = "Confirmer maître : ";
            // 
            // txtMdpMaitreConfirm
            // 
            this.txtMdpMaitreConfirm.Location = new System.Drawing.Point(156, 60);
            this.txtMdpMaitreConfirm.MaxLength = 64;
            this.txtMdpMaitreConfirm.Name = "txtMdpMaitreConfirm";
            this.txtMdpMaitreConfirm.PasswordChar = '*';
            this.txtMdpMaitreConfirm.Size = new System.Drawing.Size(322, 22);
            this.txtMdpMaitreConfirm.TabIndex = 40;
            // 
            // txtMdpJoueur
            // 
            this.txtMdpJoueur.Location = new System.Drawing.Point(156, 88);
            this.txtMdpJoueur.MaxLength = 64;
            this.txtMdpJoueur.Name = "txtMdpJoueur";
            this.txtMdpJoueur.PasswordChar = '*';
            this.txtMdpJoueur.Size = new System.Drawing.Size(322, 22);
            this.txtMdpJoueur.TabIndex = 39;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(12, 91);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(149, 17);
            this.label7.TabIndex = 38;
            this.label7.Text = "Mot de passe joueur : ";
            // 
            // txtMdpMaitre
            // 
            this.txtMdpMaitre.Location = new System.Drawing.Point(156, 34);
            this.txtMdpMaitre.MaxLength = 64;
            this.txtMdpMaitre.Name = "txtMdpMaitre";
            this.txtMdpMaitre.PasswordChar = '*';
            this.txtMdpMaitre.Size = new System.Drawing.Size(322, 22);
            this.txtMdpMaitre.TabIndex = 37;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 36);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(148, 17);
            this.label6.TabIndex = 36;
            this.label6.Text = "Mot de passe maître : ";
            // 
            // txtNomSession
            // 
            this.txtNomSession.Location = new System.Drawing.Point(142, 6);
            this.txtNomSession.MaxLength = 90;
            this.txtNomSession.Name = "txtNomSession";
            this.txtNomSession.Size = new System.Drawing.Size(336, 22);
            this.txtNomSession.TabIndex = 35;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(136, 17);
            this.label5.TabIndex = 34;
            this.label5.Text = "Nom de la session : ";
            // 
            // btCréer
            // 
            this.btCréer.Location = new System.Drawing.Point(335, 116);
            this.btCréer.Name = "btCréer";
            this.btCréer.Size = new System.Drawing.Size(145, 27);
            this.btCréer.TabIndex = 33;
            this.btCréer.Text = "Créer une session";
            this.btCréer.UseVisualStyleBackColor = true;
            this.btCréer.Click += new System.EventHandler(this.btCréer_Click);
            // 
            // CréerSession
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(491, 151);
            this.Controls.Add(this.chkBxDemanderMaitre);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.txtMdpMaitreConfirm);
            this.Controls.Add(this.txtMdpJoueur);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.txtMdpMaitre);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.txtNomSession);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.btCréer);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "CréerSession";
            this.Text = "Créer une session";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkBxDemanderMaitre;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox txtMdpMaitreConfirm;
        private System.Windows.Forms.TextBox txtMdpJoueur;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox txtMdpMaitre;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtNomSession;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btCréer;
    }
}