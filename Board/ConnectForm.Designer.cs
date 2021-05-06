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
            this.label2 = new System.Windows.Forms.Label();
            this.txtIdentifiant = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.txtPwdConnect = new System.Windows.Forms.TextBox();
            this.btConnecter = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblNomServeur
            // 
            this.lblNomServeur.AutoSize = true;
            this.lblNomServeur.Location = new System.Drawing.Point(12, 9);
            this.lblNomServeur.Name = "lblNomServeur";
            this.lblNomServeur.Size = new System.Drawing.Size(70, 17);
            this.lblNomServeur.TabIndex = 0;
            this.lblNomServeur.Text = "Serveur : ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(36, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(46, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "Port : ";
            // 
            // txtNomServeur
            // 
            this.txtNomServeur.Location = new System.Drawing.Point(88, 9);
            this.txtNomServeur.Name = "txtNomServeur";
            this.txtNomServeur.Size = new System.Drawing.Size(390, 22);
            this.txtNomServeur.TabIndex = 2;
            this.txtNomServeur.Text = "Localhost";
            // 
            // txtNumPort
            // 
            this.txtNumPort.Location = new System.Drawing.Point(88, 35);
            this.txtNumPort.Name = "txtNumPort";
            this.txtNumPort.Size = new System.Drawing.Size(390, 22);
            this.txtNumPort.TabIndex = 3;
            this.txtNumPort.Text = "8080";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(11, 65);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(81, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Identifiant : ";
            // 
            // txtIdentifiant
            // 
            this.txtIdentifiant.Location = new System.Drawing.Point(99, 62);
            this.txtIdentifiant.MaxLength = 30;
            this.txtIdentifiant.Name = "txtIdentifiant";
            this.txtIdentifiant.Size = new System.Drawing.Size(379, 22);
            this.txtIdentifiant.TabIndex = 6;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(11, 92);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(105, 17);
            this.label9.TabIndex = 25;
            this.label9.Text = "Mot de passe : ";
            // 
            // txtPwdConnect
            // 
            this.txtPwdConnect.Location = new System.Drawing.Point(117, 89);
            this.txtPwdConnect.MaxLength = 64;
            this.txtPwdConnect.Name = "txtPwdConnect";
            this.txtPwdConnect.PasswordChar = '*';
            this.txtPwdConnect.Size = new System.Drawing.Size(360, 22);
            this.txtPwdConnect.TabIndex = 20;
            // 
            // btConnecter
            // 
            this.btConnecter.Image = global::Board.Properties.Resources.connect_creating;
            this.btConnecter.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btConnecter.Location = new System.Drawing.Point(181, 119);
            this.btConnecter.Name = "btConnecter";
            this.btConnecter.Size = new System.Drawing.Size(135, 37);
            this.btConnecter.TabIndex = 4;
            this.btConnecter.Text = "Connecter";
            this.btConnecter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btConnecter.UseVisualStyleBackColor = true;
            this.btConnecter.Click += new System.EventHandler(this.btConnecter_Click);
            // 
            // ConnectForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(492, 162);
            this.Controls.Add(this.txtPwdConnect);
            this.Controls.Add(this.label9);
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
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox txtPwdConnect;
    }
}