namespace Board
{
    partial class GérerSessions
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
            this.label4 = new System.Windows.Forms.Label();
            this.txtMdpRejSession = new System.Windows.Forms.TextBox();
            this.btActualiser = new System.Windows.Forms.Button();
            this.btRejoindre = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.lstBxSessions = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(9, 282);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(101, 17);
            this.label4.TabIndex = 25;
            this.label4.Text = "Mot de passe :";
            // 
            // txtMdpRejSession
            // 
            this.txtMdpRejSession.Location = new System.Drawing.Point(115, 280);
            this.txtMdpRejSession.MaxLength = 32;
            this.txtMdpRejSession.Name = "txtMdpRejSession";
            this.txtMdpRejSession.PasswordChar = '*';
            this.txtMdpRejSession.Size = new System.Drawing.Size(250, 22);
            this.txtMdpRejSession.TabIndex = 24;
            this.txtMdpRejSession.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtMdpRejSession_KeyPress);
            // 
            // btActualiser
            // 
            this.btActualiser.Location = new System.Drawing.Point(12, 246);
            this.btActualiser.Name = "btActualiser";
            this.btActualiser.Size = new System.Drawing.Size(465, 25);
            this.btActualiser.TabIndex = 23;
            this.btActualiser.Text = "Actualiser la liste des sessions";
            this.btActualiser.UseVisualStyleBackColor = true;
            this.btActualiser.Click += new System.EventHandler(this.btActualiser_Click);
            // 
            // btRejoindre
            // 
            this.btRejoindre.Location = new System.Drawing.Point(372, 277);
            this.btRejoindre.Name = "btRejoindre";
            this.btRejoindre.Size = new System.Drawing.Size(103, 27);
            this.btRejoindre.TabIndex = 22;
            this.btRejoindre.Text = "Rejoindre";
            this.btRejoindre.UseVisualStyleBackColor = true;
            this.btRejoindre.Click += new System.EventHandler(this.btRejoindre_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 8);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(132, 17);
            this.label3.TabIndex = 21;
            this.label3.Text = "Sessions ouvertes :";
            // 
            // lstBxSessions
            // 
            this.lstBxSessions.FormattingEnabled = true;
            this.lstBxSessions.ItemHeight = 16;
            this.lstBxSessions.Location = new System.Drawing.Point(12, 28);
            this.lstBxSessions.Name = "lstBxSessions";
            this.lstBxSessions.Size = new System.Drawing.Size(463, 212);
            this.lstBxSessions.TabIndex = 20;
            this.lstBxSessions.DoubleClick += new System.EventHandler(this.lstBxSessions_DoubleClick);
            this.lstBxSessions.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lstBxSessions_MouseUp);
            // 
            // GérerSessions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(490, 312);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtMdpRejSession);
            this.Controls.Add(this.btActualiser);
            this.Controls.Add(this.btRejoindre);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.lstBxSessions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "GérerSessions";
            this.Text = "Gérer sessions";
            this.Shown += new System.EventHandler(this.GérerSessions_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtMdpRejSession;
        private System.Windows.Forms.Button btActualiser;
        private System.Windows.Forms.Button btRejoindre;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lstBxSessions;
    }
}