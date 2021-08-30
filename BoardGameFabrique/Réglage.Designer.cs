namespace BoardGameFabrique
{
    partial class Réglage
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
            this.label1 = new System.Windows.Forms.Label();
            this.tbSeuilDep = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbMarge = new System.Windows.Forms.TrackBar();
            this.tbDMin = new System.Windows.Forms.TrackBar();
            this.lbSeuilDep = new System.Windows.Forms.Label();
            this.lbMarge = new System.Windows.Forms.Label();
            this.lbDMin = new System.Windows.Forms.Label();
            this.BtValider = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.colorDialog = new System.Windows.Forms.ColorDialog();
            this.btDialogColor = new System.Windows.Forms.Button();
            this.btSuppColor = new System.Windows.Forms.Button();
            this.pnColor = new System.Windows.Forms.Panel();
            this.cbBxActDetect = new System.Windows.Forms.ComboBox();
            this.cbBxActDbDetect = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.chkBxActiverTransparence = new System.Windows.Forms.CheckBox();
            this.lbSeuil = new System.Windows.Forms.Label();
            this.tbSeuil = new System.Windows.Forms.TrackBar();
            this.label6 = new System.Windows.Forms.Label();
            this.pnTrColor = new System.Windows.Forms.Panel();
            this.btSuppColorTrans = new System.Windows.Forms.Button();
            this.btDialogColorTrans = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.lbSeuilTrans = new System.Windows.Forms.Label();
            this.tbSeuilTransparence = new System.Windows.Forms.TrackBar();
            this.label10 = new System.Windows.Forms.Label();
            this.tbArrondCoin = new System.Windows.Forms.TrackBar();
            this.label9 = new System.Windows.Forms.Label();
            this.lbArrondCoin = new System.Windows.Forms.Label();
            this.btValidSansDetect = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.tbSeuilDep)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMarge)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbDMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSeuil)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSeuilTransparence)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbArrondCoin)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(74, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(112, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "Seuil de départ :";
            // 
            // tbSeuilDep
            // 
            this.tbSeuilDep.Location = new System.Drawing.Point(192, 12);
            this.tbSeuilDep.Maximum = 400;
            this.tbSeuilDep.Name = "tbSeuilDep";
            this.tbSeuilDep.Size = new System.Drawing.Size(356, 56);
            this.tbSeuilDep.TabIndex = 1;
            this.tbSeuilDep.Value = 100;
            this.tbSeuilDep.ValueChanged += new System.EventHandler(this.tbSeuilDep_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(130, 91);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 17);
            this.label3.TabIndex = 3;
            this.label3.Text = "Marge :";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(25, 129);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(161, 17);
            this.label4.TabIndex = 4;
            this.label4.Text = "Taille minimale en pixel :";
            // 
            // tbMarge
            // 
            this.tbMarge.Location = new System.Drawing.Point(192, 91);
            this.tbMarge.Maximum = 200;
            this.tbMarge.Minimum = 1;
            this.tbMarge.Name = "tbMarge";
            this.tbMarge.Size = new System.Drawing.Size(356, 56);
            this.tbMarge.TabIndex = 6;
            this.tbMarge.Value = 15;
            this.tbMarge.ValueChanged += new System.EventHandler(this.tbMarge_ValueChanged);
            // 
            // tbDMin
            // 
            this.tbDMin.Location = new System.Drawing.Point(192, 129);
            this.tbDMin.Maximum = 500;
            this.tbDMin.Minimum = 10;
            this.tbDMin.Name = "tbDMin";
            this.tbDMin.Size = new System.Drawing.Size(356, 56);
            this.tbDMin.TabIndex = 7;
            this.tbDMin.Value = 50;
            this.tbDMin.ValueChanged += new System.EventHandler(this.tbDMin_ValueChanged);
            // 
            // lbSeuilDep
            // 
            this.lbSeuilDep.AutoSize = true;
            this.lbSeuilDep.Location = new System.Drawing.Point(554, 12);
            this.lbSeuilDep.Name = "lbSeuilDep";
            this.lbSeuilDep.Size = new System.Drawing.Size(32, 17);
            this.lbSeuilDep.TabIndex = 8;
            this.lbSeuilDep.Text = "100";
            // 
            // lbMarge
            // 
            this.lbMarge.AutoSize = true;
            this.lbMarge.Location = new System.Drawing.Point(554, 94);
            this.lbMarge.Name = "lbMarge";
            this.lbMarge.Size = new System.Drawing.Size(24, 17);
            this.lbMarge.TabIndex = 10;
            this.lbMarge.Text = "15";
            // 
            // lbDMin
            // 
            this.lbDMin.AutoSize = true;
            this.lbDMin.Location = new System.Drawing.Point(554, 133);
            this.lbDMin.Name = "lbDMin";
            this.lbDMin.Size = new System.Drawing.Size(32, 17);
            this.lbDMin.TabIndex = 11;
            this.lbDMin.Text = "150";
            // 
            // BtValider
            // 
            this.BtValider.Location = new System.Drawing.Point(168, 409);
            this.BtValider.Name = "BtValider";
            this.BtValider.Size = new System.Drawing.Size(91, 35);
            this.BtValider.TabIndex = 12;
            this.BtValider.Text = "Valider";
            this.BtValider.UseVisualStyleBackColor = true;
            this.BtValider.Click += new System.EventHandler(this.BtValider_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(33, 185);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(121, 17);
            this.label5.TabIndex = 15;
            this.label5.Text = "Couleur du fond : ";
            // 
            // btDialogColor
            // 
            this.btDialogColor.Location = new System.Drawing.Point(230, 180);
            this.btDialogColor.Name = "btDialogColor";
            this.btDialogColor.Size = new System.Drawing.Size(36, 26);
            this.btDialogColor.TabIndex = 17;
            this.btDialogColor.Text = " ...";
            this.btDialogColor.UseVisualStyleBackColor = true;
            this.btDialogColor.Click += new System.EventHandler(this.btDialogColor_Click);
            // 
            // btSuppColor
            // 
            this.btSuppColor.Location = new System.Drawing.Point(281, 180);
            this.btSuppColor.Name = "btSuppColor";
            this.btSuppColor.Size = new System.Drawing.Size(27, 26);
            this.btSuppColor.TabIndex = 18;
            this.btSuppColor.Text = "X";
            this.btSuppColor.UseVisualStyleBackColor = true;
            this.btSuppColor.Click += new System.EventHandler(this.btSuppColor_Click);
            // 
            // pnColor
            // 
            this.pnColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnColor.Location = new System.Drawing.Point(154, 181);
            this.pnColor.Name = "pnColor";
            this.pnColor.Size = new System.Drawing.Size(68, 25);
            this.pnColor.TabIndex = 19;
            // 
            // cbBxActDetect
            // 
            this.cbBxActDetect.FormattingEnabled = true;
            this.cbBxActDetect.Items.AddRange(new object[] {
            "Vide",
            "Basique",
            "Avancé"});
            this.cbBxActDetect.Location = new System.Drawing.Point(464, 181);
            this.cbBxActDetect.Name = "cbBxActDetect";
            this.cbBxActDetect.Size = new System.Drawing.Size(114, 24);
            this.cbBxActDetect.TabIndex = 23;
            this.cbBxActDetect.Text = "Avancé";
            // 
            // cbBxActDbDetect
            // 
            this.cbBxActDbDetect.FormattingEnabled = true;
            this.cbBxActDbDetect.Items.AddRange(new object[] {
            "Vide",
            "Basique",
            "Avancé"});
            this.cbBxActDbDetect.Location = new System.Drawing.Point(464, 211);
            this.cbBxActDbDetect.Name = "cbBxActDbDetect";
            this.cbBxActDbDetect.Size = new System.Drawing.Size(114, 24);
            this.cbBxActDbDetect.TabIndex = 24;
            this.cbBxActDbDetect.Text = "Basique";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(382, 185);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(76, 17);
            this.label7.TabIndex = 25;
            this.label7.Text = "Détection :";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(336, 214);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(122, 17);
            this.label8.TabIndex = 26;
            this.label8.Text = "Cadrage capture :";
            // 
            // chkBxActiverTransparence
            // 
            this.chkBxActiverTransparence.AutoSize = true;
            this.chkBxActiverTransparence.Checked = true;
            this.chkBxActiverTransparence.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkBxActiverTransparence.Location = new System.Drawing.Point(35, 255);
            this.chkBxActiverTransparence.Name = "chkBxActiverTransparence";
            this.chkBxActiverTransparence.Size = new System.Drawing.Size(177, 21);
            this.chkBxActiverTransparence.TabIndex = 27;
            this.chkBxActiverTransparence.Text = "Transparence par seuil";
            this.chkBxActiverTransparence.UseVisualStyleBackColor = true;
            // 
            // lbSeuil
            // 
            this.lbSeuil.AutoSize = true;
            this.lbSeuil.Location = new System.Drawing.Point(555, 47);
            this.lbSeuil.Name = "lbSeuil";
            this.lbSeuil.Size = new System.Drawing.Size(24, 17);
            this.lbSeuil.TabIndex = 30;
            this.lbSeuil.Text = "70";
            // 
            // tbSeuil
            // 
            this.tbSeuil.Location = new System.Drawing.Point(193, 47);
            this.tbSeuil.Maximum = 400;
            this.tbSeuil.Name = "tbSeuil";
            this.tbSeuil.Size = new System.Drawing.Size(356, 56);
            this.tbSeuil.TabIndex = 29;
            this.tbSeuil.Value = 70;
            this.tbSeuil.ValueChanged += new System.EventHandler(this.tbSeuil_ValueChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(75, 55);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(47, 17);
            this.label6.TabIndex = 28;
            this.label6.Text = "Seuil :";
            // 
            // pnTrColor
            // 
            this.pnTrColor.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.pnTrColor.Location = new System.Drawing.Point(191, 358);
            this.pnTrColor.Name = "pnTrColor";
            this.pnTrColor.Size = new System.Drawing.Size(68, 25);
            this.pnTrColor.TabIndex = 34;
            // 
            // btSuppColorTrans
            // 
            this.btSuppColorTrans.Location = new System.Drawing.Point(318, 357);
            this.btSuppColorTrans.Name = "btSuppColorTrans";
            this.btSuppColorTrans.Size = new System.Drawing.Size(27, 26);
            this.btSuppColorTrans.TabIndex = 33;
            this.btSuppColorTrans.Text = "X";
            this.btSuppColorTrans.UseVisualStyleBackColor = true;
            this.btSuppColorTrans.Click += new System.EventHandler(this.btSuppColorTrans_Click);
            // 
            // btDialogColorTrans
            // 
            this.btDialogColorTrans.Location = new System.Drawing.Point(267, 357);
            this.btDialogColorTrans.Name = "btDialogColorTrans";
            this.btDialogColorTrans.Size = new System.Drawing.Size(36, 26);
            this.btDialogColorTrans.TabIndex = 32;
            this.btDialogColorTrans.Text = " ...";
            this.btDialogColorTrans.UseVisualStyleBackColor = true;
            this.btDialogColorTrans.Click += new System.EventHandler(this.btDialogColorTrans_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(33, 362);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(157, 17);
            this.label2.TabIndex = 31;
            this.label2.Text = "Couleur transparence : ";
            // 
            // lbSeuilTrans
            // 
            this.lbSeuilTrans.AutoSize = true;
            this.lbSeuilTrans.Location = new System.Drawing.Point(534, 272);
            this.lbSeuilTrans.Name = "lbSeuilTrans";
            this.lbSeuilTrans.Size = new System.Drawing.Size(32, 17);
            this.lbSeuilTrans.TabIndex = 37;
            this.lbSeuilTrans.Text = "100";
            // 
            // tbSeuilTransparence
            // 
            this.tbSeuilTransparence.Location = new System.Drawing.Point(178, 272);
            this.tbSeuilTransparence.Maximum = 400;
            this.tbSeuilTransparence.Name = "tbSeuilTransparence";
            this.tbSeuilTransparence.Size = new System.Drawing.Size(356, 56);
            this.tbSeuilTransparence.TabIndex = 36;
            this.tbSeuilTransparence.Value = 20;
            this.tbSeuilTransparence.ValueChanged += new System.EventHandler(this.tbSeuilTransparence_ValueChanged);
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(30, 280);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(135, 17);
            this.label10.TabIndex = 35;
            this.label10.Text = "Seuil transparence :";
            // 
            // tbArrondCoin
            // 
            this.tbArrondCoin.Location = new System.Drawing.Point(178, 312);
            this.tbArrondCoin.Maximum = 1000;
            this.tbArrondCoin.Name = "tbArrondCoin";
            this.tbArrondCoin.Size = new System.Drawing.Size(356, 56);
            this.tbArrondCoin.TabIndex = 38;
            this.tbArrondCoin.Scroll += new System.EventHandler(this.tbArrondCoin_Scroll);
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(30, 316);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(152, 17);
            this.label9.TabIndex = 39;
            this.label9.Text = "Arrondissement coins :";
            // 
            // lbArrondCoin
            // 
            this.lbArrondCoin.AutoSize = true;
            this.lbArrondCoin.Location = new System.Drawing.Point(534, 317);
            this.lbArrondCoin.Name = "lbArrondCoin";
            this.lbArrondCoin.Size = new System.Drawing.Size(16, 17);
            this.lbArrondCoin.TabIndex = 40;
            this.lbArrondCoin.Text = "0";
            // 
            // btValidSansDetect
            // 
            this.btValidSansDetect.Location = new System.Drawing.Point(329, 409);
            this.btValidSansDetect.Name = "btValidSansDetect";
            this.btValidSansDetect.Size = new System.Drawing.Size(176, 35);
            this.btValidSansDetect.TabIndex = 41;
            this.btValidSansDetect.Text = "Valider sans rédétecter";
            this.btValidSansDetect.UseVisualStyleBackColor = true;
            this.btValidSansDetect.Click += new System.EventHandler(this.btValidSansDetect_Click);
            // 
            // Réglage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(593, 456);
            this.Controls.Add(this.btValidSansDetect);
            this.Controls.Add(this.lbArrondCoin);
            this.Controls.Add(this.pnTrColor);
            this.Controls.Add(this.btSuppColorTrans);
            this.Controls.Add(this.btDialogColorTrans);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tbArrondCoin);
            this.Controls.Add(this.lbSeuilTrans);
            this.Controls.Add(this.tbSeuilTransparence);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.cbBxActDetect);
            this.Controls.Add(this.pnColor);
            this.Controls.Add(this.btSuppColor);
            this.Controls.Add(this.btDialogColor);
            this.Controls.Add(this.tbDMin);
            this.Controls.Add(this.tbMarge);
            this.Controls.Add(this.lbSeuil);
            this.Controls.Add(this.tbSeuil);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.chkBxActiverTransparence);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.cbBxActDbDetect);
            this.Controls.Add(this.BtValider);
            this.Controls.Add(this.lbDMin);
            this.Controls.Add(this.lbMarge);
            this.Controls.Add(this.lbSeuilDep);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.tbSeuilDep);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label9);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Réglage";
            this.Text = "Réglages";
            ((System.ComponentModel.ISupportInitialize)(this.tbSeuilDep)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMarge)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbDMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSeuil)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbSeuilTransparence)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbArrondCoin)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar tbSeuilDep;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TrackBar tbMarge;
        private System.Windows.Forms.TrackBar tbDMin;
        private System.Windows.Forms.Label lbSeuilDep;
        private System.Windows.Forms.Label lbMarge;
        private System.Windows.Forms.Label lbDMin;
        private System.Windows.Forms.Button BtValider;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ColorDialog colorDialog;
        private System.Windows.Forms.Button btDialogColor;
        private System.Windows.Forms.Button btSuppColor;
        private System.Windows.Forms.Panel pnColor;
        private System.Windows.Forms.ComboBox cbBxActDetect;
        private System.Windows.Forms.ComboBox cbBxActDbDetect;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.CheckBox chkBxActiverTransparence;
        private System.Windows.Forms.Label lbSeuil;
        private System.Windows.Forms.TrackBar tbSeuil;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel pnTrColor;
        private System.Windows.Forms.Button btSuppColorTrans;
        private System.Windows.Forms.Button btDialogColorTrans;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lbSeuilTrans;
        private System.Windows.Forms.TrackBar tbSeuilTransparence;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TrackBar tbArrondCoin;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label lbArrondCoin;
        private System.Windows.Forms.Button btValidSansDetect;
    }
}