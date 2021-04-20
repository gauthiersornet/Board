using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ModuleBOARD.Elements.Base;
using ModuleBOARD.Réseau;

namespace Board
{
    public class ClientThreadBoard : ClientThread
    {
        private delegate void dlgVoid();
        private delegate void dlgVoidString(string str);

        private Board board;
        private ConnectForm connectForm;
        private IAsyncResult asyncResult = null;

        private LinkedList<byte[]> FileOrdres;
        private static HashSet<ServeurCodeCommande> commandesUniques = new HashSet<ServeurCodeCommande>()
        {
            ServeurCodeCommande.ActualiserSession
        };

        public ClientThreadBoard(string _identifiant, TcpClient _tcpClient, Board _board, ConnectForm _connectForm)
            :base(_tcpClient, _identifiant)
        {
            board = _board;
            connectForm = _connectForm;
            FileOrdres = new LinkedList<byte[]>();
        }

        public bool GérerConnexion()
        {
            if (connectForm != null)
            {
                connectForm.ShowDialog(board);
                return true;
            }
            return false;
        }

        private bool Identifie()
        {
            BigInteger code = ReadUBigInteger(16);
            Guid attendu = OutilsRéseau.GuidEncode(GVersion, code);
            WriteGuid(attendu);
            int res = stream.ReadByte();
            return (res == 0xFF);
        }

        private string echangerIdentifiant()
        {
            IdentifiantUL = ReadULChiffrer256();
            WriteChiffrer256(Identifiant.stringToBytes(), (OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31);
            return ReadDéChiffrer256((OutilsRéseau.NB_OCTET_NOM_UTILISATEUR_MAX + 30) / 31).bytesToString();
        }

        protected override void fonctionnement()
        {
            if (Identifie())
            {
                InitChiffrage();
                {
                    string id = echangerIdentifiant();
                    if (id != Identifiant)
                    {
                        if (asyncResult != null && asyncResult.IsCompleted == false) asyncResult.AsyncWaitHandle.WaitOne();
                        asyncResult = connectForm.BeginInvoke((dlgVoidString)(connectForm.IdentifiantRefusé), id);
                        Identifiant = id;
                    }
                }

                etat = EClientThreadEtat.Connecté;
                if (asyncResult != null && asyncResult.IsCompleted == false) asyncResult.AsyncWaitHandle.WaitOne();
                asyncResult = connectForm.BeginInvoke((dlgVoid)(connectForm.ConnectionRéussie));
                asyncResult.AsyncWaitHandle.WaitOne();
                asyncResult = null;

                // Tant que le thread n'est pas tué, on travaille
                while (Thread.CurrentThread.IsAlive && fonctionne && tcpClient.Connected)
                {
                    while (stream.DataAvailable)
                    {
                        byte[] block = ReadDéChiffrer256();
                        if (block != null)
                        {
                            switch ((BoardCodeCommande)block[0])
                            {
                                case BoardCodeCommande.AjouterSession:
                                    block = ReadDéChiffrer256Data(block, 2);
                                    if (block != null)
                                    {
                                        if (asyncResult != null && asyncResult.IsCompleted == false) asyncResult.AsyncWaitHandle.WaitOne();
                                        asyncResult = connectForm.BeginInvoke((dlgVoidString)(connectForm.AjouterSession), block.bytesToString());
                                    }
                                    break;
                                default:
                                    Close();
                                    fonctionne = false;
                                    break;
                            }
                        }
                    }

                    lock (FileOrdres)
                    {
                        if(FileOrdres.Any())
                        {
                            byte[] ord = FileOrdres.First.Value;
                            FileOrdres.RemoveFirst();
                            WriteChiffrer256(ord);

                            continue;
                        }
                    }

                    // Attente de 10 ms
                    if (stream.DataAvailable == false) Thread.Sleep(100);
                }

                connectForm.BeginInvoke((dlgVoidString)(connectForm.PerteDeConnexion), "Déconnexion");
            }
            else
            {
                if (asyncResult != null && asyncResult.IsCompleted == false) asyncResult.AsyncWaitHandle.WaitOne();
                asyncResult = connectForm.BeginInvoke((dlgVoidString)(connectForm.ConnectionRattée), "Votre version n'est pas compatible.");
            }

            stream.Close();
            stream = null;
            tcpClient.Close();
            tcpClient = null;
        }

        new protected void Close()
        {
            base.Close();
        }

        public void Envoyer(ServeurCodeCommande cmd, byte[] data)
        {
            byte[] nb = new byte[(data?.Length ?? 0) + 1];
            nb[0] = (byte)cmd;
            if (data != null && data.Length > 0) Array.Copy(data, 0, nb, 1, data.Length);

            lock (FileOrdres)
            {
                if (commandesUniques.Contains(cmd) && FileOrdres.Any())
                {
                    LinkedListNode<byte[]> iter;
                    for (iter = FileOrdres.First; iter != null; iter = iter.Next)
                    {
                        if (iter.Value[0] == (byte)cmd)
                        {
                            iter.Value = nb;
                            break;
                        }
                    }
                    if(iter == null) FileOrdres.AddLast(nb);
                }
                else FileOrdres.AddLast(nb);
            }
        }

        public void ActualiserLstSession()
        {
            /*if(etat == EClientThreadEtat.Connecté)
            {
                WriteChiffrer256(ServeurCodeCommande.ActualiserSession, null);
            }*/
            Envoyer(ServeurCodeCommande.ActualiserSession, null);
        }
    }
}
