using ModuleBOARD.Réseau;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BoardServer
{
    public class Program
    {
        static void Main(string[] args)
        {
            //new BoardSession("Kosmopolite", null, OutilsRéseau.BIntHashPassword256(""), OutilsRéseau.BIntHashPassword256(""), false);
            new BoardSession("GameOver", null, OutilsRéseau.BIntHashPassword256("g861"), OutilsRéseau.BIntHashPassword256(""), false);
            new BoardSession("Virus", null, OutilsRéseau.BIntHashPassword256("g861"), OutilsRéseau.BIntHashPassword256(""), false);

            Int32 port;
            string sPort = args.FirstOrDefault(s => s.StartsWith("port="));
            if (sPort != null) port = Int32.Parse(sPort.Substring("port=".Length).Trim());
            else port = 8080;

            string ipAdresse = args.FirstOrDefault(s => s.StartsWith("ip="));
            if (ipAdresse != null) ipAdresse = ipAdresse.Substring("ip=".Length).Trim();
            else ipAdresse = "127.0.0.1";

            BigInteger hashpwd; ;
            string mdp = args.FirstOrDefault(s => s.StartsWith("mdp="));
            if (mdp != null) hashpwd = OutilsRéseau.BIntHashPassword256(mdp.Substring("mdp=".Length).Trim());
            else hashpwd = OutilsRéseau.BIntHashPassword256("");

            ConcurrentQueue<string> Message = new ConcurrentQueue<string>();
            TimeSpan délaisIdentification = new TimeSpan(0, 2, 0);
            TimeSpan délaisFonctionnement = new TimeSpan(1, 0, 0);

            TcpListener server = null;
            try
            {
                IPAddress localAddr = IPAddress.Parse(ipAdresse);
                server = new TcpListener(localAddr, port);

                // Lancer l'écoute
                server.Start();
                Console.Write("En attente de connexion... ");

                int nbClient = 0;

                // Boucle d'écoute.
                while (true)
                {
                    if (server.Pending())
                    {
                        TcpClient client = server.AcceptTcpClient();
                        
                        if (client != null)
                        {
                            Console.WriteLine("Connexion!");
                            ++nbClient;
                            lock (ClientThreadServer.lstClientThread)
                            {
                                if (ClientThreadServer.lstClientThread.Count < 20)
                                    new ClientThreadServer(client, hashpwd, Message).Lancer();
                                else client.Close();
                            }
                        }
                    }

                    string str;
                    if (Message.TryDequeue(out str))Console.WriteLine(str);

                    lock (ClientThreadServer.lstClientThread)
                    {
                        List<ClientThreadServer> clientOutLst = null;
                        foreach(var clt in ClientThreadServer.lstClientThread)
                        {
                            if(clt.EstIdentifié)
                            {
                                if(TimeSpan.Compare(DateTime.Now - clt.DateDernierFonctionnement, délaisFonctionnement) > 0)
                                {
                                    if (clientOutLst == null) clientOutLst = new List<ClientThreadServer>();
                                    clientOutLst.Add(clt);
                                    try { clt.Close(); } catch { }
                                }
                            }
                            else if(clt.EstConnecté)
                            {
                                if (TimeSpan.Compare(DateTime.Now - clt.DateDernierFonctionnement, délaisIdentification) > 0)
                                {
                                    if (clientOutLst == null) clientOutLst = new List<ClientThreadServer>();
                                    clientOutLst.Add(clt);
                                    try { clt.Close(); } catch { }
                                }
                            }
                            else
                            {
                                if (clientOutLst == null) clientOutLst = new List<ClientThreadServer>();
                                clientOutLst.Add(clt);
                            }
                        }

                        if (clientOutLst != null)
                        {
                            foreach(ClientThreadServer clt in clientOutLst)
                            {
                                ClientThreadServer.lstClientThread.Remove(clt);
                                if (!clt.SafeStop()) clt.Abort();
                            }
                        }

                        if (ClientThreadServer.lstClientThread.Count != nbClient)
                        {
                            nbClient = ClientThreadServer.lstClientThread.Count;
                            Console.WriteLine("Il reste " + nbClient + " clients connectés.");
                        }
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("Connexion exception: {0}", e);
            }
            finally
            {
                // Arrête de l'écoute
                server.Stop();
            }
        }
    }
}
