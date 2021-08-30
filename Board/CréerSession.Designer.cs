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
            this.chkBoxDroits = new System.Windows.Forms.CheckedListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbBxProfilTable = new System.Windows.Forms.ComboBox();
            this.cbBxChatAudioCodec = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // chkBxDemanderMaitre
            // 
            this.chkBxDemanderMaitre.AutoSize = true;
            this.chkBxDemanderMaitre.Checked = true;
            this.chkBxDemanderMaitre.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBxDemanderMaitre.Location = new System.Drawing.Point(15, 301);
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
            this.txtMdpMaitreConfirm.TabIndex = 2;
            this.txtMdpMaitreConfirm.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Session_KeyPress);
            // 
            // txtMdpJoueur
            // 
            this.txtMdpJoueur.Location = new System.Drawing.Point(156, 88);
            this.txtMdpJoueur.MaxLength = 64;
            this.txtMdpJoueur.Name = "txtMdpJoueur";
            this.txtMdpJoueur.PasswordChar = '*';
            this.txtMdpJoueur.Size = new System.Drawing.Size(322, 22);
            this.txtMdpJoueur.TabIndex = 3;
            this.txtMdpJoueur.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Session_KeyPress);
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
            this.txtMdpMaitre.TabIndex = 1;
            this.txtMdpMaitre.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Session_KeyPress);
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
            this.txtNomSession.TabIndex = 0;
            this.txtNomSession.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Session_KeyPress);
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
            this.btCréer.Location = new System.Drawing.Point(176, 340);
            this.btCréer.Name = "btCréer";
            this.btCréer.Size = new System.Drawing.Size(145, 27);
            this.btCréer.TabIndex = 4;
            this.btCréer.Text = "Créer une session";
            this.btCréer.UseVisualStyleBackColor = true;
            this.btCréer.Click += new System.EventHandler(this.btCréer_Click);
            // 
            // chkBoxDroits
            // 
            this.chkBoxDroits.FormattingEnabled = true;
            this.chkBoxDroits.Location = new System.Drawing.Point(216, 124);
            this.chkBoxDroits.Name = "chkBoxDroits";
            this.chkBoxDroits.Size = new System.Drawing.Size(262, 123);
            this.chkBoxDroits.TabIndex = 43;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 126);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(204, 17);
            this.label1.TabIndex = 44;
            this.label1.Text = "Droits par défaut des joueurs : ";
            // 
            // cbBxProfilTable
            // 
            this.cbBxProfilTable.FormattingEnabled = true;
            this.cbBxProfilTable.Items.AddRange(new object[] {
            "Personnalisé",
            "Tout les droits",
            "Passage de main simple",
            "Passage de main complet",
            "Aucun droits"});
            this.cbBxProfilTable.Location = new System.Drawing.Point(12, 149);
            this.cbBxProfilTable.Name = "cbBxProfilTable";
            this.cbBxProfilTable.Size = new System.Drawing.Size(198, 24);
            this.cbBxProfilTable.TabIndex = 45;
            this.cbBxProfilTable.SelectedIndexChanged += new System.EventHandler(this.cbBxProfilTable_SelectedIndexChanged);
            // 
            // cbBxChatAudioCodec
            // 
            this.cbBxChatAudioCodec.FormattingEnabled = true;
            this.cbBxChatAudioCodec.Location = new System.Drawing.Point(139, 261);
            this.cbBxChatAudioCodec.Name = "cbBxChatAudioCodec";
            this.cbBxChatAudioCodec.Size = new System.Drawing.Size(340, 24);
            this.cbBxChatAudioCodec.TabIndex = 46;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 266);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(121, 17);
            this.label2.TabIndex = 47;
            this.label2.Text = "ChatAudioCodec :";
            // 
            // CréerSession
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(489, 379);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cbBxChatAudioCodec);
            this.Controls.Add(this.cbBxProfilTable);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chkBoxDroits);
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
            this.TopMost = true;
            this.Shown += new System.EventHandler(this.CréerSession_Shown);
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
        private System.Windows.Forms.CheckedListBox chkBoxDroits;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbBxProfilTable;
        private System.Windows.Forms.ComboBox cbBxChatAudioCodec;
        private System.Windows.Forms.Label label2;
    }
}