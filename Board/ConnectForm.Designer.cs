namespace Board
{
    partial class ConnectForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConnectForm));
            this.lblNomServeur = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtNomServeur = new System.Windows.Forms.TextBox();
            this.txtNumPort = new System.Windows.Forms.TextBox();
            this.btConnecter = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.txtIdentifiant = new System.Windows.Forms.TextBox();
            this.lstBxSessions = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.btRejoindre = new System.Windows.Forms.Button();
            this.btActualiser = new System.Windows.Forms.Button();
            this.btCréer = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblNomServeur
            // 
            this.lblNomServeur.AutoSize = true;
            this.lblNomServeur.Location = new System.Drawing.Point(12, 45);
            this.lblNomServeur.Name = "lblNomServeur";
            this.lblNomServeur.Size = new System.Drawing.Size(70, 17);
            this.lblNomServeur.TabIndex = 0;
            this.lblNomServeur.Text = "Serveur : ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Port : ";
            // 
            // txtNomServeur
            // 
            this.txtNomServeur.Location = new System.Drawing.Point(88, 45);
            this.txtNomServeur.Name = "txtNomServeur";
            this.txtNomServeur.Size = new System.Drawing.Size(390, 22);
            this.txtNomServeur.TabIndex = 2;
            this.txtNomServeur.Text = "Localhost";
            // 
            // txtNumPort
            // 
            this.txtNumPort.Location = new System.Drawing.Point(88, 70);
            this.txtNumPort.Name = "txtNumPort";
            this.txtNumPort.Size = new System.Drawing.Size(390, 22);
            this.txtNumPort.TabIndex = 3;
            this.txtNumPort.Text = "8080";
            // 
            // btConnecter
            // 
            this.btConnecter.Image = global::Board.Properties.Resources.connect_creating;
            this.btConnecter.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btConnecter.Location = new System.Drawing.Point(181, 106);
            this.btConnecter.Name = "btConnecter";
            this.btConnecter.Size = new System.Drawing.Size(135, 37);
            this.btConnecter.TabIndex = 4;
            this.btConnecter.Text = "Connecter";
            this.btConnecter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btConnecter.UseVisualStyleBackColor = true;
            this.btConnecter.Click += new System.EventHandler(this.btConnecter_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Identifiant : ";
            // 
            // txtIdentifiant
            // 
            this.txtIdentifiant.Location = new System.Drawing.Point(99, 9);
            this.txtIdentifiant.Name = "txtIdentifiant";
            this.txtIdentifiant.Size = new System.Drawing.Size(379, 22);
            this.txtIdentifiant.TabIndex = 6;
            // 
            // lstBxSessions
            // 
            this.lstBxSessions.Enabled = false;
            this.lstBxSessions.FormattingEnabled = true;
            this.lstBxSessions.ItemHeight = 16;
            this.lstBxSessions.Location = new System.Drawing.Point(15, 189);
            this.lstBxSessions.Name = "lstBxSessions";
            this.lstBxSessions.Size = new System.Drawing.Size(463, 212);
            this.lstBxSessions.TabIndex = 7;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 169);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(132, 17);
            this.label3.TabIndex = 8;
            this.label3.Text = "Sessions ouvertes :";
            // 
            // btRejoindre
            // 
            this.btRejoindre.Enabled = false;
            this.btRejoindre.Location = new System.Drawing.Point(15, 407);
            this.btRejoindre.Name = "btRejoindre";
            this.btRejoindre.Size = new System.Drawing.Size(140, 27);
            this.btRejoindre.TabIndex = 9;
            this.btRejoindre.Text = "Rejoindre";
            this.btRejoindre.UseVisualStyleBackColor = true;
            // 
            // btActualiser
            // 
            this.btActualiser.Enabled = false;
            this.btActualiser.Location = new System.Drawing.Point(176, 407);
            this.btActualiser.Name = "btActualiser";
            this.btActualiser.Size = new System.Drawing.Size(140, 27);
            this.btActualiser.TabIndex = 10;
            this.btActualiser.Text = "Actualiser";
            this.btActualiser.UseVisualStyleBackColor = true;
            this.btActualiser.Click += new System.EventHandler(this.btActualiser_Click);
            // 
            // btCréer
            // 
            this.btCréer.Enabled = false;
            this.btCréer.Location = new System.Drawing.Point(338, 407);
            this.btCréer.Name = "btCréer";
            this.btCréer.Size = new System.Drawing.Size(140, 27);
            this.btCréer.TabIndex = 11;
            this.btCréer.Text = "Créer";
            this.btCréer.UseVisualStyleBackColor = true;
            // 
            // ConnectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 444);
            this.Controls.Add(this.btCréer);
            this.Controls.Add(this.btActualiser);
            this.Controls.Add(this.btRejoindre);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lstBxSessions);
            this.Controls.Add(this.txtIdentifiant);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btConnecter);
            this.Controls.Add(this.txtNumPort);
            this.Controls.Add(this.txtNomServeur);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblNomServeur);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "ConnectForm";
            this.Text = "Connexion";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblNomServeur;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtNomServeur;
        private System.Windows.Forms.TextBox txtNumPort;
        private System.Windows.Forms.Button btConnecter;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtIdentifiant;
        private System.Windows.Forms.ListBox lstBxSessions;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btRejoindre;
        private System.Windows.Forms.Button btActualiser;
        private System.Windows.Forms.Button btCréer;
    }
}