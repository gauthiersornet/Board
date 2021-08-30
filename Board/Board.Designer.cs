namespace Board
{
    partial class Board
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Board));
            this.tabControle = new System.Windows.Forms.TabControl();
            this.tabConsole = new System.Windows.Forms.TabPage();
            this.rTxtMessageConsole = new System.Windows.Forms.RichTextBox();
            this.splitter4 = new System.Windows.Forms.Splitter();
            this.richTextBox3 = new System.Windows.Forms.RichTextBox();
            this.splitter3 = new System.Windows.Forms.Splitter();
            this.lvConsGJrs = new System.Windows.Forms.ListView();
            this.tabGénérale = new System.Windows.Forms.TabPage();
            this.rTxtMessageGénéral = new System.Windows.Forms.RichTextBox();
            this.splitter2 = new System.Windows.Forms.Splitter();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this.rTxtSaisieGénéral = new System.Windows.Forms.RichTextBox();
            this.lvGJrs = new System.Windows.Forms.ListView();
            this.tabSession = new System.Windows.Forms.TabPage();
            this.rTxtMessageSession = new System.Windows.Forms.RichTextBox();
            this.splitter6 = new System.Windows.Forms.Splitter();
            this.splitter5 = new System.Windows.Forms.Splitter();
            this.lvTableJr = new System.Windows.Forms.ListView();
            this.rTxtSaisieSession = new System.Windows.Forms.RichTextBox();
            this.splitter7 = new System.Windows.Forms.Splitter();
            this.tabControle.SuspendLayout();
            this.tabConsole.SuspendLayout();
            this.tabGénérale.SuspendLayout();
            this.tabSession.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControle
            // 
            this.tabControle.Alignment = System.Windows.Forms.TabAlignment.Bottom;
            this.tabControle.Controls.Add(this.tabConsole);
            this.tabControle.Controls.Add(this.tabGénérale);
            this.tabControle.Controls.Add(this.tabSession);
            this.tabControle.Dock = System.Windows.Forms.DockStyle.Right;
            this.tabControle.Location = new System.Drawing.Point(1117, 0);
            this.tabControle.Multiline = true;
            this.tabControle.Name = "tabControle";
            this.tabControle.SelectedIndex = 0;
            this.tabControle.Size = new System.Drawing.Size(322, 729);
            this.tabControle.TabIndex = 0;
            this.tabControle.TabStop = false;
            // 
            // tabConsole
            // 
            this.tabConsole.BackColor = System.Drawing.SystemColors.Control;
            this.tabConsole.Controls.Add(this.rTxtMessageConsole);
            this.tabConsole.Controls.Add(this.splitter4);
            this.tabConsole.Controls.Add(this.richTextBox3);
            this.tabConsole.Controls.Add(this.splitter3);
            this.tabConsole.Controls.Add(this.lvConsGJrs);
            this.tabConsole.Location = new System.Drawing.Point(4, 4);
            this.tabConsole.Name = "tabConsole";
            this.tabConsole.Padding = new System.Windows.Forms.Padding(3);
            this.tabConsole.Size = new System.Drawing.Size(314, 700);
            this.tabConsole.TabIndex = 0;
            this.tabConsole.Text = "Console";
            // 
            // rTxtMessageConsole
            // 
            this.rTxtMessageConsole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rTxtMessageConsole.Location = new System.Drawing.Point(3, 291);
            this.rTxtMessageConsole.Name = "rTxtMessageConsole";
            this.rTxtMessageConsole.ReadOnly = true;
            this.rTxtMessageConsole.Size = new System.Drawing.Size(308, 299);
            this.rTxtMessageConsole.TabIndex = 13;
            this.rTxtMessageConsole.TabStop = false;
            this.rTxtMessageConsole.Text = "";
            // 
            // splitter4
            // 
            this.splitter4.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter4.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter4.Location = new System.Drawing.Point(3, 590);
            this.splitter4.Name = "splitter4";
            this.splitter4.Size = new System.Drawing.Size(308, 10);
            this.splitter4.TabIndex = 11;
            this.splitter4.TabStop = false;
            // 
            // richTextBox3
            // 
            this.richTextBox3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.richTextBox3.Location = new System.Drawing.Point(3, 600);
            this.richTextBox3.MaxLength = 1024;
            this.richTextBox3.Name = "richTextBox3";
            this.richTextBox3.Size = new System.Drawing.Size(308, 97);
            this.richTextBox3.TabIndex = 8;
            this.richTextBox3.Text = "";
            this.richTextBox3.KeyDown += new System.Windows.Forms.KeyEventHandler(this.rTxt_KeyDown);
            this.richTextBox3.KeyUp += new System.Windows.Forms.KeyEventHandler(this.rTxt_KeyUp);
            // 
            // splitter3
            // 
            this.splitter3.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter3.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter3.Location = new System.Drawing.Point(3, 281);
            this.splitter3.Name = "splitter3";
            this.splitter3.Size = new System.Drawing.Size(308, 10);
            this.splitter3.TabIndex = 10;
            this.splitter3.TabStop = false;
            // 
            // lvConsGJrs
            // 
            this.lvConsGJrs.Dock = System.Windows.Forms.DockStyle.Top;
            this.lvConsGJrs.HideSelection = false;
            this.lvConsGJrs.Location = new System.Drawing.Point(3, 3);
            this.lvConsGJrs.Name = "lvConsGJrs";
            this.lvConsGJrs.Size = new System.Drawing.Size(308, 278);
            this.lvConsGJrs.TabIndex = 9;
            this.lvConsGJrs.UseCompatibleStateImageBehavior = false;
            this.lvConsGJrs.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvConsGJrs_MouseUp);
            // 
            // tabGénérale
            // 
            this.tabGénérale.BackColor = System.Drawing.SystemColors.Control;
            this.tabGénérale.Controls.Add(this.rTxtMessageGénéral);
            this.tabGénérale.Controls.Add(this.splitter2);
            this.tabGénérale.Controls.Add(this.splitter1);
            this.tabGénérale.Controls.Add(this.rTxtSaisieGénéral);
            this.tabGénérale.Controls.Add(this.lvGJrs);
            this.tabGénérale.Location = new System.Drawing.Point(4, 4);
            this.tabGénérale.Name = "tabGénérale";
            this.tabGénérale.Padding = new System.Windows.Forms.Padding(3);
            this.tabGénérale.Size = new System.Drawing.Size(314, 700);
            this.tabGénérale.TabIndex = 1;
            this.tabGénérale.Text = "Général";
            // 
            // rTxtMessageGénéral
            // 
            this.rTxtMessageGénéral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rTxtMessageGénéral.Location = new System.Drawing.Point(3, 291);
            this.rTxtMessageGénéral.Name = "rTxtMessageGénéral";
            this.rTxtMessageGénéral.ReadOnly = true;
            this.rTxtMessageGénéral.Size = new System.Drawing.Size(308, 299);
            this.rTxtMessageGénéral.TabIndex = 13;
            this.rTxtMessageGénéral.TabStop = false;
            this.rTxtMessageGénéral.Text = "";
            // 
            // splitter2
            // 
            this.splitter2.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter2.Location = new System.Drawing.Point(3, 590);
            this.splitter2.Name = "splitter2";
            this.splitter2.Size = new System.Drawing.Size(308, 10);
            this.splitter2.TabIndex = 14;
            this.splitter2.TabStop = false;
            // 
            // splitter1
            // 
            this.splitter1.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter1.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter1.Location = new System.Drawing.Point(3, 281);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(308, 10);
            this.splitter1.TabIndex = 12;
            this.splitter1.TabStop = false;
            // 
            // rTxtSaisieGénéral
            // 
            this.rTxtSaisieGénéral.CausesValidation = false;
            this.rTxtSaisieGénéral.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.rTxtSaisieGénéral.Location = new System.Drawing.Point(3, 600);
            this.rTxtSaisieGénéral.MaxLength = 1024;
            this.rTxtSaisieGénéral.Name = "rTxtSaisieGénéral";
            this.rTxtSaisieGénéral.Size = new System.Drawing.Size(308, 97);
            this.rTxtSaisieGénéral.TabIndex = 5;
            this.rTxtSaisieGénéral.Text = "";
            this.rTxtSaisieGénéral.KeyDown += new System.Windows.Forms.KeyEventHandler(this.rTxt_KeyDown);
            this.rTxtSaisieGénéral.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.rTxtSaisieGénéral_KeyPress);
            this.rTxtSaisieGénéral.KeyUp += new System.Windows.Forms.KeyEventHandler(this.rTxt_KeyUp);
            // 
            // lvGJrs
            // 
            this.lvGJrs.Dock = System.Windows.Forms.DockStyle.Top;
            this.lvGJrs.HideSelection = false;
            this.lvGJrs.Location = new System.Drawing.Point(3, 3);
            this.lvGJrs.Name = "lvGJrs";
            this.lvGJrs.Size = new System.Drawing.Size(308, 278);
            this.lvGJrs.TabIndex = 11;
            this.lvGJrs.UseCompatibleStateImageBehavior = false;
            this.lvGJrs.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvGJrs_MouseUp);
            // 
            // tabSession
            // 
            this.tabSession.BackColor = System.Drawing.SystemColors.Control;
            this.tabSession.Controls.Add(this.rTxtMessageSession);
            this.tabSession.Controls.Add(this.splitter6);
            this.tabSession.Controls.Add(this.splitter5);
            this.tabSession.Controls.Add(this.lvTableJr);
            this.tabSession.Controls.Add(this.rTxtSaisieSession);
            this.tabSession.Location = new System.Drawing.Point(4, 4);
            this.tabSession.Name = "tabSession";
            this.tabSession.Padding = new System.Windows.Forms.Padding(3);
            this.tabSession.Size = new System.Drawing.Size(314, 700);
            this.tabSession.TabIndex = 2;
            this.tabSession.Text = "Session";
            // 
            // rTxtMessageSession
            // 
            this.rTxtMessageSession.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rTxtMessageSession.Location = new System.Drawing.Point(3, 291);
            this.rTxtMessageSession.Name = "rTxtMessageSession";
            this.rTxtMessageSession.ReadOnly = true;
            this.rTxtMessageSession.Size = new System.Drawing.Size(308, 299);
            this.rTxtMessageSession.TabIndex = 10;
            this.rTxtMessageSession.TabStop = false;
            this.rTxtMessageSession.Text = "";
            // 
            // splitter6
            // 
            this.splitter6.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter6.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.splitter6.Location = new System.Drawing.Point(3, 590);
            this.splitter6.Name = "splitter6";
            this.splitter6.Size = new System.Drawing.Size(308, 10);
            this.splitter6.TabIndex = 9;
            this.splitter6.TabStop = false;
            // 
            // splitter5
            // 
            this.splitter5.Cursor = System.Windows.Forms.Cursors.HSplit;
            this.splitter5.Dock = System.Windows.Forms.DockStyle.Top;
            this.splitter5.Location = new System.Drawing.Point(3, 281);
            this.splitter5.Name = "splitter5";
            this.splitter5.Size = new System.Drawing.Size(308, 10);
            this.splitter5.TabIndex = 8;
            this.splitter5.TabStop = false;
            // 
            // lvTableJr
            // 
            this.lvTableJr.Dock = System.Windows.Forms.DockStyle.Top;
            this.lvTableJr.HideSelection = false;
            this.lvTableJr.Location = new System.Drawing.Point(3, 3);
            this.lvTableJr.Name = "lvTableJr";
            this.lvTableJr.Size = new System.Drawing.Size(308, 278);
            this.lvTableJr.TabIndex = 6;
            this.lvTableJr.UseCompatibleStateImageBehavior = false;
            this.lvTableJr.MouseUp += new System.Windows.Forms.MouseEventHandler(this.lvTableJr_MouseUp);
            // 
            // rTxtSaisieSession
            // 
            this.rTxtSaisieSession.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.rTxtSaisieSession.Location = new System.Drawing.Point(3, 600);
            this.rTxtSaisieSession.MaxLength = 1024;
            this.rTxtSaisieSession.Name = "rTxtSaisieSession";
            this.rTxtSaisieSession.Size = new System.Drawing.Size(308, 97);
            this.rTxtSaisieSession.TabIndex = 5;
            this.rTxtSaisieSession.Text = "";
            this.rTxtSaisieSession.KeyDown += new System.Windows.Forms.KeyEventHandler(this.rTxt_KeyDown);
            this.rTxtSaisieSession.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.rTxtSaisieSession_KeyPress);
            this.rTxtSaisieSession.KeyUp += new System.Windows.Forms.KeyEventHandler(this.rTxt_KeyUp);
            // 
            // splitter7
            // 
            this.splitter7.Cursor = System.Windows.Forms.Cursors.NoMoveHoriz;
            this.splitter7.Dock = System.Windows.Forms.DockStyle.Right;
            this.splitter7.Location = new System.Drawing.Point(1109, 0);
            this.splitter7.Name = "splitter7";
            this.splitter7.Size = new System.Drawing.Size(8, 729);
            this.splitter7.TabIndex = 1;
            this.splitter7.TabStop = false;
            // 
            // Board
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnablePreventFocusChange;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1439, 729);
            this.Controls.Add(this.splitter7);
            this.Controls.Add(this.tabControle);
            this.DoubleBuffered = true;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Board";
            this.Text = "Board";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Board_FormClosing);
            this.Load += new System.EventHandler(this.Board_Load);
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.Board_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.Board_DragEnter);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Board_KeyDown);
            this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.Board_KeyPress);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Board_KeyUp);
            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.Board_MouseClick);
            this.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.Board_MouseDoubleClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Board_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Board_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Board_MouseUp);
            this.Resize += new System.EventHandler(this.Board_Resize);
            this.tabControle.ResumeLayout(false);
            this.tabConsole.ResumeLayout(false);
            this.tabGénérale.ResumeLayout(false);
            this.tabSession.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControle;
        private System.Windows.Forms.TabPage tabConsole;
        private System.Windows.Forms.TabPage tabGénérale;
        private System.Windows.Forms.TabPage tabSession;
        private System.Windows.Forms.ListView lvTableJr;
        private System.Windows.Forms.RichTextBox rTxtSaisieSession;
        private System.Windows.Forms.RichTextBox rTxtSaisieGénéral;
        private System.Windows.Forms.RichTextBox rTxtMessageGénéral;
        private System.Windows.Forms.Splitter splitter2;
        private System.Windows.Forms.Splitter splitter1;
        private System.Windows.Forms.ListView lvGJrs;
        private System.Windows.Forms.RichTextBox rTxtMessageConsole;
        private System.Windows.Forms.Splitter splitter4;
        private System.Windows.Forms.RichTextBox richTextBox3;
        private System.Windows.Forms.Splitter splitter3;
        private System.Windows.Forms.ListView lvConsGJrs;
        private System.Windows.Forms.RichTextBox rTxtMessageSession;
        private System.Windows.Forms.Splitter splitter6;
        private System.Windows.Forms.Splitter splitter5;
        private System.Windows.Forms.Splitter splitter7;
    }
}

