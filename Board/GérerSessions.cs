using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Board
{
    public partial class GérerSessions : Form
    {
        public string sessionChoisie = null;

        private ClientThreadBoard clientThreadBoard;

        public GérerSessions(ClientThreadBoard cb)
        {
            this.DialogResult = DialogResult.No;
            clientThreadBoard = cb;
            InitializeComponent();
            clientThreadBoard.ActualiserLstSession();
        }

        public void AjouterSession(string nomSession)
        {
            if (nomSession == "") lstBxSessions.Items.Clear();
            else lstBxSessions.Items.Add(nomSession);
        }

        private void btActualiser_Click(object sender, EventArgs e)
        {
            clientThreadBoard.ActualiserLstSession();
        }

        private void btRejoindre_Click(object sender, EventArgs e)
        {
            sessionChoisie = lstBxSessions.SelectedItem.ToString();
            this.DialogResult = DialogResult.Yes;
        }

        private void SupprimerSession()
        {
            if (lstBxSessions.SelectedIndex >= 0)
            {
                if (clientThreadBoard != null)
                {
                    if (MessageBox.Show("Ëtes-vous sûr de vouloir supprimer la session \"" + lstBxSessions.SelectedItem + "\" ?", "Supprimer session ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        //lstBxSessions.Items.RemoveAt(lstBxSessions.SelectedIndex);
                        clientThreadBoard.SupprimerSession(lstBxSessions.SelectedItem.ToString());
                    }
                }
                else MessageBox.Show("Vous êtes non connecté.", "Pas de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void lstBxSessions_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button.HasFlag(MouseButtons.Right) && lstBxSessions.SelectedIndex >= 0)
            {
                ContextMenu ctxM = new ContextMenu();
                ctxM.MenuItems.Add(new MenuItem("Supprimer", (o, eArg) => { this.SupprimerSession(); }));
                ctxM.Show(this, e.Location);
            }
        }
    }
}
