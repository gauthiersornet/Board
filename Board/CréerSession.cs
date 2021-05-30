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
    public partial class CréerSession : Form
    {
        private ClientThreadBoard clientThreadBoard;

        public string NomSession = null;
        public BigInteger SessionHashPwd = 0;
        private Point p;

        public CréerSession(ClientThreadBoard ct, Point p)
        {
            this.p = p;
            clientThreadBoard = ct;
            InitializeComponent();
            this.DialogResult = DialogResult.No;
        }

        private void btCréer_Click(object sender, EventArgs e)
        {
            txtNomSession.Text = txtNomSession.Text.Trim();
            if (String.IsNullOrWhiteSpace(txtNomSession.Text))
            {
                MessageBox.Show("Veuillez saisir un nom de session.", "Erreur nom session", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (txtNomSession.Text.Length > OutilsRéseau.NB_OCTET_NOM_SESSION_MAX ||
                UTF8Encoding.UTF8.GetBytes(txtNomSession.Text).Length > OutilsRéseau.NB_OCTET_NOM_SESSION_MAX)
            {
                MessageBox.Show("Votre nom de session est trop long.", "Erreur nom session", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (OutilsRéseau.EstChaineSecurisée(txtNomSession.Text) == false)
            {
                MessageBox.Show("Votre nom de session contient des caractères non authorisés.", "Erreur nom session", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if(txtMdpMaitre.Text != txtMdpMaitreConfirm.Text)
            {
                MessageBox.Show("Votre mot de passe maître n'est pas identique à sa confirmation.", "Erreur mot de passe maître", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            BigInteger hashMotDePasseMaitre = OutilsRéseau.BIntHashPassword256(txtMdpMaitre.Text);
            BigInteger hashMotDePasseJoueur = OutilsRéseau.BIntHashPassword256(txtMdpJoueur.Text);

            if(clientThreadBoard.EstIdentifié)
            {
                if (MessageBox.Show("Êtes-vous sûr de vouloir créer cette session \"" + txtNomSession.Text + "\"", "Créer session ?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    clientThreadBoard.CréerSession(txtNomSession.Text, hashMotDePasseMaitre, hashMotDePasseJoueur, chkBxDemanderMaitre.Checked);
                    NomSession = txtNomSession.Text;
                    SessionHashPwd = hashMotDePasseMaitre;
                    this.DialogResult = DialogResult.Yes;
                }
            }
            else MessageBox.Show("Vous êtes déconnecté.", "Erreur de connexion", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void CréerSession_Shown(object sender, EventArgs e)
        {
            this.Left = p.X - this.Width / 2;
            this.Top = p.Y - this.Height / 2;
        }
    }
}
