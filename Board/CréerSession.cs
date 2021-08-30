using ModuleBOARD.Réseau;
using NAudioDemo.NetworkChatDemo;
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

        private ItemCheckEventHandler itChkEvt;

        public CréerSession(ClientThreadBoard ct, Point p)
        {
            this.p = p;
            clientThreadBoard = ct;
            InitializeComponent();
            this.DialogResult = DialogResult.No;
            for(int i = 0; i < 16 && Enum.GetName(typeof(Joueur.EDroits), 1 << i) != null; ++i)
                chkBoxDroits.Items.Add(Enum.GetName(typeof(Joueur.EDroits), (Joueur.EDroits)(1 << i)));
            cbBxProfilTable.SelectedIndex = 1;
            itChkEvt = new ItemCheckEventHandler(this.chkBoxDroits_ItemCheck);
            chkBoxDroits.ItemCheck += itChkEvt;

            var codecs = NAudioDemo.Utils.ReflectionHelper.CreateAllInstancesOf<INetworkChatCodec>();
            PopulateCodecsCombo(codecs);
        }

        private void PopulateCodecsCombo(IEnumerable<INetworkChatCodec> codecs)
        {
            var sorted = from codec in codecs
                         where codec.IsAvailable
                         orderby codec.BitsPerSecond ascending
                         select codec;

            cbBxChatAudioCodec.Items.Add(new CodecComboItem { Text = "Aucun", Codec = null });
            foreach (var codec in sorted)
            {
                var bitRate = codec.BitsPerSecond == -1 ? "VBR" : $"{codec.BitsPerSecond / 1000.0:0.#}kbps";
                var text = $"{codec.Name} ({bitRate})";
                cbBxChatAudioCodec.Items.Add(new CodecComboItem { Text = text, Codec = codec });
            }
            cbBxChatAudioCodec.SelectedIndex = 0;
        }

        private class CodecComboItem
        {
            public string Text { get; set; }
            public INetworkChatCodec Codec { get; set; }
            public override string ToString()
            {
                return Text;
            }
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
                    Joueur.EDroits droits = Joueur.EDroits.Néant;
                    for (int i = 0; i < chkBoxDroits.Items.Count; ++i) if(chkBoxDroits.GetItemChecked(i)) droits |= (Joueur.EDroits)(1 << i);
                    INetworkChatCodec codec = ((CodecComboItem)cbBxChatAudioCodec.SelectedItem).Codec;
                    clientThreadBoard.CréerSession(txtNomSession.Text, hashMotDePasseMaitre, hashMotDePasseJoueur, chkBxDemanderMaitre.Checked, (codec?.Name ?? ""), droits);
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

        private void Session_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
                btCréer_Click(sender, e);
        }

        private int Droit2Index(Joueur.EDroits d)
        {
            for (int i = 0; i < 16; ++i)
                if (((Joueur.EDroits)(1 << i)) == d) return i;
            return -1;
        }

        private void cbBxProfilTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cbBxProfilTable.SelectedIndex > 0)
            {
                chkBoxDroits.ItemCheck -= itChkEvt;
                for (int i = 0; i < chkBoxDroits.Items.Count; ++i) chkBoxDroits.SetItemChecked(i, false);
                switch (cbBxProfilTable.SelectedIndex)
                {
                    case 1: //Tout les droits
                        for (int i = 0; i < chkBoxDroits.Items.Count; ++i) chkBoxDroits.SetItemChecked(i, true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.GestDroits), false);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.PasseMain), false);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Bloqué), false);
                        break;
                    case 2: //Passage de main simple
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.PasseMain), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Attraper), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Piocher), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Retourner), true);
                        break;
                    case 3: //Passage de main complet
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.PasseMain), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Attraper), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Piocher), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.Retourner), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.AttraperLots), true);
                        chkBoxDroits.SetItemChecked(Droit2Index(Joueur.EDroits.RetournerLots), true);
                        break;
                    case 4: //Aucun droits
                        break;
                }
                chkBoxDroits.ItemCheck += itChkEvt;
            }
        }

        private void chkBoxDroits_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            cbBxProfilTable.SelectedIndex = 0;
        }
    }
}
