namespace BoardGameFabrique
{
    partial class Sauvegarder
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
            this.txtTailleFinalW = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lblTMoyW = new System.Windows.Forms.Label();
            this.scrolPourMille = new System.Windows.Forms.HScrollBar();
            this.label3 = new System.Windows.Forms.Label();
            this.lblTMoyH = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblTaillePourc = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.txtTailleFinalH = new System.Windows.Forms.TextBox();
            this.scrolBarQualité = new System.Windows.Forms.HScrollBar();
            this.label4 = new System.Windows.Forms.Label();
            this.lblQualit = new System.Windows.Forms.Label();
            this.chkBxGénérerXML = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // btValid
            // 
            this.btValid.Location = new System.Drawing.Point(189, 209);
            this.btValid.Name = "btValid";
            this.btValid.Size = new System.Drawing.Size(80, 32);
            this.btValid.TabIndex = 2;
            this.btValid.Text = "Valider";
            this.btValid.UseVisualStyleBackColor = true;
            this.btValid.Click += new System.EventHandler(this.btValid_Click);
            // 
            // txtTailleFinalW
            // 
            this.txtTailleFinalW.Location = new System.Drawing.Point(189, 113);
            this.txtTailleFinalW.Name = "txtTailleFinalW";
            this.txtTailleFinalW.Size = new System.Drawing.Size(124, 22);
            this.txtTailleFinalW.TabIndex = 0;
            this.txtTailleFinalW.Text = "224";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(25, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(182, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Taille moyenne des cartes :";
            // 
            // lblTMoyW
            // 
            this.lblTMoyW.AutoSize = true;
            this.lblTMoyW.Location = new System.Drawing.Point(213, 20);
            this.lblTMoyW.Name = "lblTMoyW";
            this.lblTMoyW.Size = new System.Drawing.Size(80, 17);
            this.lblTMoyW.TabIndex = 5;
            this.lblTMoyW.Text = "100000000";
            this.lblTMoyW.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // scrolPourMille
            // 
            this.scrolPourMille.LargeChange = 1;
            this.scrolPourMille.Location = new System.Drawing.Point(28, 79);
            this.scrolPourMille.Maximum = 1000;
            this.scrolPourMille.Minimum = 1;
            this.scrolPourMille.Name = "scrolPourMille";
            this.scrolPourMille.Size = new System.Drawing.Size(439, 24);
            this.scrolPourMille.TabIndex = 6;
            this.scrolPourMille.Value = 1000;
            this.scrolPourMille.ValueChanged += new System.EventHandler(this.scrolPourMille_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(299, 20);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(14, 17);
            this.label3.TabIndex = 7;
            this.label3.Text = "x";
            // 
            // lblTMoyH
            // 
            this.lblTMoyH.AutoSize = true;
            this.lblTMoyH.Location = new System.Drawing.Point(319, 20);
            this.lblTMoyH.Name = "lblTMoyH";
            this.lblTMoyH.Size = new System.Drawing.Size(80, 17);
            this.lblTMoyH.TabIndex = 9;
            this.lblTMoyH.Text = "100000000";
            this.lblTMoyH.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(25, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(302, 17);
            this.label2.TabIndex = 10;
            this.label2.Text = "Taille finale pourcentage de la taille moyenne :";
            // 
            // lblTaillePourc
            // 
            this.lblTaillePourc.AutoSize = true;
            this.lblTaillePourc.Location = new System.Drawing.Point(333, 53);
            this.lblTaillePourc.Name = "lblTaillePourc";
            this.lblTaillePourc.Size = new System.Drawing.Size(56, 17);
            this.lblTaillePourc.TabIndex = 11;
            this.lblTaillePourc.Text = "100.0%";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(25, 113);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(158, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Taille finale des cartes :";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(323, 118);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(14, 17);
            this.label6.TabIndex = 13;
            this.label6.Text = "x";
            // 
            // txtTailleFinalH
            // 
            this.txtTailleFinalH.Location = new System.Drawing.Point(343, 113);
            this.txtTailleFinalH.Name = "txtTailleFinalH";
            this.txtTailleFinalH.Size = new System.Drawing.Size(124, 22);
            this.txtTailleFinalH.TabIndex = 1;
            this.txtTailleFinalH.Text = "312";
            // 
            // scrolBarQualité
            // 
            this.scrolBarQualité.LargeChange = 1;
            this.scrolBarQualité.Location = new System.Drawing.Point(92, 145);
            this.scrolBarQualité.Minimum = 1;
            this.scrolBarQualité.Name = "scrolBarQualité";
            this.scrolBarQualité.Size = new System.Drawing.Size(300, 24);
            this.scrolBarQualité.TabIndex = 15;
            this.scrolBarQualité.Value = 100;
            this.scrolBarQualité.ValueChanged += new System.EventHandler(this.scrolBarQualité_ValueChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(25, 147);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 17);
            this.label4.TabIndex = 16;
            this.label4.Text = "Qualité :";
            // 
            // lblQualit
            // 
            this.lblQualit.AutoSize = true;
            this.lblQualit.Location = new System.Drawing.Point(411, 148);
            this.lblQualit.Name = "lblQualit";
            this.lblQualit.Size = new System.Drawing.Size(44, 17);
            this.lblQualit.TabIndex = 17;
            this.lblQualit.Text = "100%";
            // 
            // chkBxGénérerXML
            // 
            this.chkBxGénérerXML.AutoSize = true;
            this.chkBxGénérerXML.Checked = true;
            this.chkBxGénérerXML.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBxGénérerXML.Location = new System.Drawing.Point(28, 172);
            this.chkBxGénérerXML.Name = "chkBxGénérerXML";
            this.chkBxGénérerXML.Size = new System.Drawing.Size(130, 21);
            this.chkBxGénérerXML.TabIndex = 18;
            this.chkBxGénérerXML.Text = "Générer le XML";
            this.chkBxGénérerXML.UseVisualStyleBackColor = true;
            // 
            // Sauvegarder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(478, 253);
            this.Controls.Add(this.chkBxGénérerXML);
            this.Controls.Add(this.lblQualit);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.scrolBarQualité);
            this.Controls.Add(this.txtTailleFinalH);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblTaillePourc);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblTMoyH);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.scrolPourMille);
            this.Controls.Add(this.lblTMoyW);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtTailleFinalW);
            this.Controls.Add(this.btValid);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Sauvegarder";
            this.Text = "Sauvegarder";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btValid;
        private System.Windows.Forms.TextBox txtTailleFinalW;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblTMoyW;
        private System.Windows.Forms.HScrollBar scrolPourMille;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblTMoyH;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblTaillePourc;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtTailleFinalH;
        private System.Windows.Forms.HScrollBar scrolBarQualité;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label lblQualit;
        private System.Windows.Forms.CheckBox chkBxGénérerXML;
    }
}