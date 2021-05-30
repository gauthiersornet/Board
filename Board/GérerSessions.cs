using ModuleBOARD.Réseau;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Board
{
    public partial class GérerSessions : Form
    {
        public string SessionChoisie = null;

        private ClientThreadBoard clientThreadBoard;
        private Point p;

        public GérerSessions(ClientThreadBoard cb, Point p, string session = null)
        {
            this.p = p;
            this.DialogResult = DialogResult.No;
            clientThreadBoard = cb;
            InitializeComponent();
            SessionChoisie = clientThreadBoard.NomSession;
            Remettre(session);
            clientThreadBoard.ActualiserLstSession();
        }

        private void Remettre(string session = null)
        {
            if (!String.IsNullOrWhiteSpace(SessionChoisie))
            {
                this.Text = "Gérer sessions (" + SessionChoisie + ")";
                txtMdpRejSession.Enabled = false;
                btRejoindre.Text = "Quitter";
            }
            else
            {
                SessionChoisie = session;
                this.Text = "Gérer sessions";
                txtMdpRejSession.Enabled = true;
                btRejoindre.Text = "Rejoindre";
            }
        }

        public void ActualiserSessions(List<string> nomSessions)
        {
            lstBxSessions.Items.Clear();
            lstBxSessions.Items.AddRange(nomSessions.ToArray());
            if (SessionChoisie !=  null && lstBxSessions.Items.Contains(SessionChoisie))
            {
                lstBxSessions.SelectedItem = SessionChoisie;
            }
            else SessionChoisie = null;
        }

        private void btActualiser_Click(object sender, EventArgs e)
        {
            clientThreadBoard.ActualiserLstSession();
        }

        private void btRejoindre_Click(object sender, EventArgs e)
        {
            if (!clientThreadBoard.EstDansSession)
            {
                if (lstBxSessions.SelectedItem != null && MessageBox.Show("Effacer la table ?", "Votre table sera éffacées, êtes-vous sur de vouloir joindre la session ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    SessionChoisie = lstBxSessions.SelectedItem.ToString();
                    BigInteger hash = OutilsRéseau.BIntHashPassword256(txtMdpRejSession.Text);
                    clientThreadBoard.EnqueueCommande(ModuleBOARD.Réseau.ClientThread.ServeurCodeCommande.RejoindreSession, SessionChoisie, hash);
                    this.DialogResult = DialogResult.Yes;
                }
            }
            else // c'est une déco !
            {
                if (MessageBox.Show("Quitter la session en cours", "Êtes-vous sûr de vouloir quitter la session \"" + SessionChoisie + "\" ?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    clientThreadBoard.EnqueueCommande(ModuleBOARD.Réseau.ClientThread.ServeurCodeCommande.QuitterSession);
                    this.DialogResult = DialogResult.Yes;
                }
            }
        }

        private void SupprimerSession()
        {
            if (lstBxSessions.SelectedIndex >= 0)
            {
                if (clientThreadBoard != null && clientThreadBoard.EstIdentifié)
                {
                    if (MessageBox.Show("Ëtes-vous sûr de vouloir supprimer la session \"" + lstBxSessions.SelectedItem + "\" ?", "Supprimer session ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        //lstBxSessions.Items.RemoveAt(lstBxSessions.SelectedIndex);
                        clientThreadBoard.SupprimerSession(lstBxSessions.SelectedItem.ToString());
                        if (String.Equals(SessionChoisie, lstBxSessions.SelectedItem))
                        {
                            SessionChoisie = null;
                            Remettre();
                        }
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

        private void GérerSessions_Shown(object sender, EventArgs e)
        {
            this.Left = p.X - this.Width / 2;
            this.Top = p.Y - this.Height / 2;
        }

        private void lstBxSessions_DoubleClick(object sender, EventArgs e)
        {
            btRejoindre_Click(sender, e);
        }

        private void txtMdpRejSession_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == 13)
            {
                btRejoindre_Click(sender, e);
            }
        }
    }
}
