using ModuleBOARD.Elements.Base;
using ModuleBOARD.Elements.Lots;
using ModuleBOARD.Elements.Lots.Piles;
using ModuleBOARD.Elements.Pieces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ModuleBOARD.Réseau
{
    public interface IRessourcesDésérialiseur
    {
        BibliothèqueImage BibImage { get; }
        BibliothèqueModel BibModel { get; }

        void NouvelleElement(IBinSerialisable elm);
        object Rechercher(int idElement);
        object RetrouverObject(Stream stream);
    }

    public enum EType : byte
    {
        Vide = 0,
        Element = 1,
        Element2D = 2,
        Element2D2F = 3,
        Figurine = 4,
        Groupe = 5,
        Paquet = 6,
        Pile = 7,
        Pioche = 8,
        Défausse = 9,

        Joueur = 20,
        ElémentRéseau = 21
    }

    public interface IBinSerialisable
    {
        int ElmId { get; set; }
        EType ElmType { get; }
        void Serialiser(Stream stream, ref int gidr);//On envoit que l'élément
        void SerialiserTout(Stream stream, ref int gidr, ISet<int> setIdRéseau);//on commence par envoyer les sous-éléments (si pas dans setIdRéseau) puis l'élément

        object MettreAJour(object obj);
    }

    public static class OutilsRéseau
    {
        public const int NB_OCTET_MAX = 10 * 1024 * 1024;

        public const int NB_OCTET_STR_MAX = 1024;

        public const int NB_WPixel_MAX = 1048576; // 2^20
        public const int NB_HPixel_MAX = 1048576; // 2^20

        public const int NB_OCTET_NOM_UTILISATEUR_MAX = 31 - 1;

        public const int NB_OCTET_NOM_SESSION_MAX = 3 * 31 - 1 - 1;

        public const int NB_OCTET_COMMANDE = 1;

        static public bool EstChaineSecurisée(this string str)
        {
            return str.All(c => char.IsSeparator(c) ||
                                char.IsPunctuation(c) ||
                                char.IsLetterOrDigit(c));
        }

        static public bool IdRéseauVacant(this int IdentifiantRéseau, int gidr)
        {
            /*if (gidr > 0)//On est sur de la sérialisation serveur
                return (IdentifiantRéseau <= 0);//ne devrait jamais arriver...
            if (gidr < 0)//On est sur de la sérialisation client
                return (IdentifiantRéseau == 0);
            else return false;*/
            return (IdentifiantRéseau == 0);
        }

        static public int IdRéseauSuivant(ref this int gidr)
        {
            int res = gidr;
            if (gidr > 0)//On est sur de la sérialisation serveur
            {
                if (gidr == int.MaxValue) gidr = 1;
                else ++gidr;
            }
            else if (gidr < 0)//On est sur de la sérialisation client
            {
                if (gidr == int.MaxValue) gidr = -1;
                else --gidr;
            }
            return res;
        }

        static public readonly BigInteger Prime256Bits = new BigInteger(new byte[]
            {0x2F, 0xFC , 0xFF, 0xFF , 0xFE , 0xFF , 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF, 0x00});

        static public readonly BigInteger Max256Bits = new BigInteger(new byte[]
            {0xFF, 0xFF , 0xFF, 0xFF , 0xFF , 0xFF , 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF, 0x00});

        static public readonly BigInteger Max128Bits = new BigInteger(new byte[]
            {0xFF, 0xFF , 0xFF, 0xFF , 0xFF , 0xFF , 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF , 0xFF, 0xFF, 0xFF, 0xFF, 0x00});

        static public BigInteger FastPowModR(BigInteger a, BigInteger e, BigInteger m)
        {
            if (e == 0) return 1;
            else
            {
                BigInteger rv = FastPowModR(a, e >> 1, m);
                if ((e.ToByteArray()[0] & 1) == 1) return (rv * rv * a) % m;
                else return (rv * rv) % m;
            }
        }

        static public BigInteger FastPowMod(BigInteger a, BigInteger e, BigInteger m)
        {
            BigInteger res = 1;
            byte[] eBts = e.ToByteArray();
            for (int i = eBts.Length - 1; i >= 0; --i)
            {
                for (int j = 7; j >= 0; --j)
                {
                    res = (res * res);
                    if ((eBts[i] & (1 << j)) != 0) res *= a;
                    res %= m;
                }
            }
            return res;
        }

        static public BigInteger FastPowMod256(BigInteger g, BigInteger p)
        {
            return FastPowMod(g, p, Prime256Bits);
        }

        static public BigInteger RandomUBigInteger(int nbBytes, Random rnd = null)
        {
            if (rnd == null) rnd = new Random();
            byte[] bts = new byte[nbBytes+1];
            rnd.NextBytes(bts);
            return new BigInteger(bts);
        }

        static public BigInteger SecuredRandomBigInteger128(Random rnd = null)
        {
            BigInteger res;
            do
            {
                if (rnd == null) rnd = new Random();
                BigInteger a = 0;
                while (a <= 1000 || Prime256Bits <= a) a = RandomUBigInteger(16, rnd);
                BigInteger b = 0;
                while (b <= 1000) b = RandomUBigInteger(16, rnd);
                res = FastPowMod(a, b, Prime256Bits) % Max128Bits;
            } while (res == BigInteger.Zero);
            return res;
        }

        static public BigInteger SecuredRandomBigInteger256(Random rnd = null)
        {
            BigInteger res;
            do
            {
                if (rnd == null) rnd = new Random();
                BigInteger a = 0;
                while (a <= 1000 || Prime256Bits <= a) a = RandomUBigInteger(32, rnd);
                BigInteger b = 0;
                while (b <= 1000) b = RandomUBigInteger(32, rnd);
                res = FastPowMod(a, b, Prime256Bits);
            } while (res == BigInteger.Zero);
            return res;
        }

        static public BigInteger ChargerUBigInteger(byte[] bInt)
        {
            if (bInt != null && bInt.Length > 0)
            {
                if ((bInt[bInt.Length - 1] & 0x80) != 0)
                {
                    byte[] ncode = new byte[bInt.Length + 1];
                    Array.Copy(bInt, ncode, bInt.Length);
                    bInt = ncode;
                }
                return new BigInteger(bInt);
            }
            else return new BigInteger();
        }

        static public Guid GuidEncode(Guid guid, byte[] code)
        {
            return GuidEncode(guid, ChargerUBigInteger(code));
        }

        static public Guid GuidEncode(Guid guid, BigInteger bintCode)
        {
            bintCode += 1000;
            BigInteger bintGuid = ChargerUBigInteger(guid.ToByteArray()) + 1000;
            BigInteger bintRes = FastPowMod(bintGuid, bintCode, Prime256Bits);
            byte[] rBytes = bintRes.ToByteArray();
            byte[] rBytes16 = new byte[16];
            Array.Copy(rBytes, rBytes16, Math.Min(rBytes.Length, rBytes16.Length));
            return new Guid(rBytes16);
        }

        static public byte[] stringToBytes(this string s)
        {
            byte[] sb = UTF8Encoding.UTF8.GetBytes(s);
            byte[] res = new byte[sb.Length + 1];
            Array.Copy(sb, 0, res, 0, sb.Length);
            res[res.Length - 1] = 0;
            return res;
        }

        static public string bytesToString(this byte[] bytes)
        {
            if (bytes != null)
            {
                int idx;
                for (idx = 0; idx < bytes.Length && bytes[idx] != 0; ++idx) ;//On cherche le dernier 0
                return UTF8Encoding.UTF8.GetString(bytes, 0, idx);
            }
            else return null;
        }

        static public byte[] AddFirst(this byte[] data, byte cmd)
        {
            /*byte[] nb = new byte[(data?.Length ?? 0) + 1];
            nb[0] = (byte) cmd;
            if (data != null && data.Length > 0) Array.Copy(data, 0, nb, 1, data.Length);
            return nb;*/
            int len = data.Length;
            Array.Resize(ref data, len + 1);
            Array.Copy(data, 0, data, 1, len);
            data[0] = cmd;
            return data;
        }

        static public byte[] SubBytes(this byte[] bts, int idx)
        {
            return bts.SubBytes(idx, bts.Length - idx);
        }

        static public byte[] SubBytes(this byte[] bts, int idx, int len)
        {
            /*byte[] nb = new byte[len];
            Array.Copy(bts, idx, nb, 0, len);
            return nb;*/
            Array.Copy(bts, idx, bts, 0, len);
            Array.Resize(ref bts, len);
            return bts;
        }

        static public byte[] AddBytes(this byte[] a, byte[] b)
        {
            /*int la = (a?.Length ?? 0);
            int lb = (b?.Length ?? 0);
            byte[] nb = new byte[la + lb];
            if (la > 0) Array.Copy(a, 0, nb, 0, la);
            if (lb > 0) Array.Copy(b, 0, nb, la, lb);
            return nb;*/
            int la = (a?.Length ?? 0);
            int lb = (b?.Length ?? 0);
            Array.Resize(ref a, la + lb);
            if (lb > 0) Array.Copy(b, 0, a, la, lb);
            return a;
        }

        static public byte[] AddLast(this byte[] a, byte b)
        {
            /*int la = (a?.Length ?? 0);
            byte[] nb = new byte[la + 1];
            if (la > 0) Array.Copy(a, 0, nb, 0, la);
            nb[la] = b;
            return nb;*/
            int len = a.Length;
            Array.Resize(ref a, len + 1);
            a[len] = b;
            return a;
        }

        static public byte[] AddBytes(this byte[] a, byte[] b, int len)
        {
            return AddBytes(a,b, 0, len);
        }

        static public byte[] AddBytes(this byte[] a, byte[] b, int idx, int len)
        {
            /*
            int la = (a?.Length ?? 0);
            int lb = Math.Min(len, b.Length - idx);
            byte[] nb = new byte[la + len];
            if (la > 0) Array.Copy(a, 0, nb, 0, la);
            if(lb > 0) Array.Copy(b, idx, a, la, lb);
            return nb;*/
            int la = (a?.Length ?? 0);
            int lb = Math.Min(len, b.Length - idx);
            Array.Resize(ref a, la + lb);
            if(lb > 0) Array.Copy(b, idx, a, la, lb);
            return a;
        }

        /*static public byte[] SubFuse(this byte[] a, int aidx, int alen, byte[] b, int bidx, int blen)
        {
            if (alen < 0) alen = 0;
            if (blen < 0) blen = 0;
            byte[] nb = new byte[alen + blen];
            if (alen > 0) Array.Copy(a, aidx, nb, 0, alen);
            if (blen > 0) Array.Copy(b, bidx, nb, alen, blen);
            return nb;
        }*/

        static public byte[] HashPassword256(string pwd)
        {
            return SHA256.Create().ComputeHash(UTF8Encoding.UTF8.GetBytes(pwd));
        }

        /*static public void MaskerHash256(byte[] hash256)
        {
            //hash256[4] &= 0xFE;
            //hash256[1] &= 0xFC;
            //hash256[0] &= 0x2E;
            hash256[31] = 0;
        }*/

        static public BigInteger HashBytesToBigInt(byte[] hash256)
        {
            //Application d'un mask afin d'être sur que le hash sera inférieur à Prime256Bits
            //en effet, si celui ci devait être supérieur ou égale alors le modulo l'annulerait et on perdrait l'information !
            //hash256[31] &= 0x7F;
            //Le hash sur 31 bytes est largement suffisant et doit passer dans un paquet 32
            hash256[31] = 0;
            return new BigInteger(hash256);
        }

        static public BigInteger BIntHashPassword256(string pwd)
        {
            return HashBytesToBigInt(HashPassword256(pwd));
        }

        /*static public byte[] ToBytes(this object o)
        {

        }*/

        static public Stream SerialiserElement(this Stream stream, Element e, ref int gidr)
        {
            //stream.WriteByte((byte)e.ElmType);
            e.Serialiser(stream, ref gidr);
            return stream;
        }

        /*static public Stream SerialiserElementTout(this Stream stream, Element e, ref int gidr, ISet<int> setIdRéseau)
        {
            e.SerialiserTout(stream, ref gidr, setIdRéseau);
            return stream;
        }*/

        static public Stream SerialiserTout(this Stream stream, Element e, ref int gidr, ISet<int> setIdRéseau)
        {
            if(e != null)
            {
                if (e.IdRéseauVacant(gidr))
                {
                    e.IdentifiantRéseau = int.MaxValue;
                    if (e.IdentifiantRéseau == int.MaxValue)
                    {
                        e.IdentifiantRéseau = gidr.IdRéseauSuivant();
                        setIdRéseau.Add(e.IdentifiantRéseau);
                        //stream.SerialiserElementTout(e, ref gidr, setIdRéseau);
                    }
                    e.SerialiserTout(stream, ref gidr, setIdRéseau);
                }
                else if(setIdRéseau.Contains(e.IdentifiantRéseau) == false)
                {
                    setIdRéseau.Add(e.IdentifiantRéseau);
                    //stream.SerialiserElementTout(e, ref gidr, setIdRéseau);
                    e.SerialiserTout(stream, ref gidr, setIdRéseau);
                }
            }
            return stream;
        }


        /// <summary>
        /// On ne sérialise que la référence de l'élément
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="e"></param>
        /// <param name="gidr"></param>
        /// <returns></returns>
        static public Stream SerialiserRefElement(this Stream stream, Element e, ref int gidr)
        {
            int refId;
            if (e != null)
            {
                if (e.IdRéseauVacant(gidr))
                {
                    e.IdentifiantRéseau = int.MaxValue;
                    if(e.IdentifiantRéseau == int.MaxValue)
                        e.IdentifiantRéseau = gidr.IdRéseauSuivant();
                }
                refId = e.IdentifiantRéseau;
            }
            else refId = 0;
            stream.Serialiser(BitConverter.GetBytes(refId));
            return stream;
        }

        static public Stream Serialiser(this Stream stream, byte[] bts)
        {
            stream.Write(bts, 0, bts.Length);
            return stream;
        }

        static public Stream SerialiserObject(this Stream stream, object o, ref int gidr, ISet<int> setIdRéseau, Type type = null)
        {
            IBinSerialisable ibin = o as IBinSerialisable;
            if (ibin != null)
            {
                if (ibin.ElmId.IdRéseauVacant(gidr))
                {
                    ibin.ElmId = int.MaxValue;//Test si le set est possible
                    if(ibin.ElmId == int.MaxValue)
                        ibin.ElmId = gidr.IdRéseauSuivant();
                }
                if(ibin.ElmId != 0) setIdRéseau.Add(ibin.ElmId);
                ibin.SerialiserTout(stream, ref gidr, setIdRéseau);
                return stream;
            }
            else
            {
                if (type == null)
                {
                    if (o != null) type = o.GetType();
                    else return stream;
                }
                if (type.IsArray)
                {
                    //Array...
                    Type atype = type.GetElementType();
                    Array tab = o as Array;
                    ushort len = (ushort)(tab?.Length ?? 0);
                    stream.WriteBytes(BitConverter.GetBytes(len));
                    for (ushort i = 0; i < len; ++i)
                        stream.SerialiserObject(tab.GetValue(i), ref gidr, setIdRéseau, atype);
                    return stream;
                }
                else if (type.IsGenericType)
                {
                    if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
                    {
                        //Liste
                        if (o != null)
                        {
                            Type atype = type.GenericTypeArguments.First();
                            for (IEnumerator etor = (o as IEnumerable).GetEnumerator(); etor.MoveNext();)
                                stream.SerialiserObject(etor.Current, ref gidr, setIdRéseau, atype);
                        }
                        return stream;
                    }
                    throw new Exception("Problème de sérialisation binaire d'un objet.");
                }
                else return SerialiserObject(stream, o);
            }
        }

        static public Stream SerialiserParRéférence(this Stream stream, ref int gidr, params IBinSerialisable[] tabIbins)
        {
            ushort nbIBin = (ushort)(tabIbins?.Length ?? 0);
            stream.WriteBytes(BitConverter.GetBytes(nbIBin));
            for(ushort i = 0; i < nbIBin; ++i)
                stream.WriteBytes(BitConverter.GetBytes(tabIbins[i].ElmId));
            return stream;
        }

        static public Stream SerialiserObject(this Stream stream, object o, ref int gidr, Type type = null)
        {
            IBinSerialisable ibin = o as IBinSerialisable;
            if (ibin != null)
            {
                if (ibin.ElmId.IdRéseauVacant(gidr))
                {
                    ibin.ElmId = int.MaxValue;//Test si le set est possible
                    if (ibin.ElmId == int.MaxValue)
                        ibin.ElmId = gidr.IdRéseauSuivant();
                }
                ibin.Serialiser(stream, ref gidr);
                return stream;
            }
            else
            {
                if (type == null)
                {
                    if (o != null) type = o.GetType();
                    else return stream;
                }
                if (type.IsArray)
                {
                    //Array...
                    Type atype = type.GetElementType();
                    Array tab = o as Array;
                    ushort len = (ushort)(tab?.Length ?? 0);
                    stream.WriteBytes(BitConverter.GetBytes(len));
                    for (ushort i = 0; i < len; ++i)
                        stream.SerialiserObject(tab.GetValue(i), ref gidr, atype);
                    return stream;
                }
                else if (type.IsGenericType)
                {
                    if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
                    {
                        //Liste
                        if (o != null)
                        {
                            Type atype = type.GenericTypeArguments.First();
                            for (IEnumerator etor = (o as IEnumerable).GetEnumerator(); etor.MoveNext();)
                                stream.SerialiserObject(etor.Current, ref gidr, atype);
                        }
                        return stream;
                    }
                    throw new Exception("Problème de sérialisation binaire d'un objet.");
                }
                else return SerialiserObject(stream, o);
            }
        }

        static public Stream SerialiserObject(this Stream stream, object o, Type type = null)
        {
            if (type == null)
            {
                if (o != null) type = o.GetType();
                else return stream;
            }
            if (type.IsEnum)
                return stream.SerialiserObject(Convert.ChangeType(o, Enum.GetUnderlyingType(o.GetType())));
            else if (type.IsArray)
            {
                //Array...
                Type atype = type.GetElementType();
                Array tab = o as Array;
                ushort len = (ushort)(tab?.Length ?? 0);
                stream.WriteBytes(BitConverter.GetBytes(len));
                for (ushort i = 0; i < len; ++i)
                    SerialiserObject(stream, tab.GetValue(i), atype);
                return stream;
            }
            else if (type.IsGenericType)
            {
                if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
                {
                    //Liste
                    if (o != null)
                    {
                        Type atype = type.GenericTypeArguments.First();
                        for (IEnumerator etor = (o as IEnumerable).GetEnumerator(); etor.MoveNext();)
                            stream.SerialiserObject(etor.Current, atype);
                    }
                    return stream;
                }
                throw new Exception("Problème de sérialisation binaire d'un objet.");
            }
            else
            {
                byte[] buff;
                if (o is Image)
                    buff = UTF8Encoding.UTF8.GetBytes((o as Image).Tag as string).AddLast(0);
                else if (o is GeoCoord2D)
                    buff = BitConverter.GetBytes(((GeoCoord2D)o).P.X).AddBytes(BitConverter.GetBytes(((GeoCoord2D)o).P.Y)).AddBytes(BitConverter.GetBytes(((GeoCoord2D)o).E)).AddBytes(BitConverter.GetBytes(((GeoCoord2D)o).A));
                else if (o is Point)
                    buff = BitConverter.GetBytes(((Point)o).X).AddBytes(BitConverter.GetBytes(((Point)o).Y));
                else if (o is PointF)
                    buff = BitConverter.GetBytes(((PointF)o).X).AddBytes(BitConverter.GetBytes(((PointF)o).Y));
                else if (o is Int64)
                    buff = BitConverter.GetBytes((Int64)o);
                else if (o is Int32)
                    buff = BitConverter.GetBytes((Int32)o);
                else if (o is Int16)
                    buff = BitConverter.GetBytes((Int16)o);
                else if (o is UInt64)
                    buff = BitConverter.GetBytes((UInt64)o);
                else if (o is UInt32)
                    buff = BitConverter.GetBytes((UInt32)o);
                else if (o is UInt16)
                    buff = BitConverter.GetBytes((UInt16)o);
                else if (o is Char)
                    buff = BitConverter.GetBytes((Char)o);
                else if (o is Byte)
                    buff = new byte[1] { (Byte)o };
                else if (o is SByte)
                    buff = new byte[1] { (Byte)o };
                else if (o is Boolean)
                    buff = BitConverter.GetBytes((Boolean)o);
                else if (o is Single)
                    buff = BitConverter.GetBytes((Single)o);
                else if (o is Double)
                    buff = BitConverter.GetBytes((Double)o);
                else if (o is String)
                    buff = UTF8Encoding.UTF8.GetBytes(o as string).AddLast(0);
                else if (o is BigInteger)//Il s'agit d'un hash256
                {
                    buff = ((BigInteger)o).ToByteArray();
                    Array.Resize(ref buff, 31);
                }
                else throw new Exception("Problème de sérialisation binaire d'un objet.");

                stream.Write(buff, 0, buff.Length);
            }
            return stream;
        }

        static public byte[] GetBytes(this Stream stream, int len)
        {
            byte[] bts = new byte[len];
            int nb = stream.Read(bts, 0, len);
            return (nb == len ? bts : null);
        }

        static public Stream WriteBytes(this Stream stream, byte[] bts)
        {
            stream.Write(bts, 0, bts.Length);
            return stream;
        }

        static public GeoCoord2D ReadGeoCoord2D(this Stream stream)
        {
            return new GeoCoord2D(new PointF(BitConverter.ToSingle(stream.GetBytes(4), 0), BitConverter.ToSingle(stream.GetBytes(4), 0)), BitConverter.ToSingle(stream.GetBytes(4), 0), BitConverter.ToSingle(stream.GetBytes(4), 0));
        }

        static public Point ReadPoint(this Stream stream)
        {
            return new Point(BitConverter.ToInt32(stream.GetBytes(4), 0), BitConverter.ToInt32(stream.GetBytes(4), 0));
        }

        static public PointF ReadPointF(this Stream stream)
        {
            return new PointF(BitConverter.ToSingle(stream.GetBytes(4), 0), BitConverter.ToSingle(stream.GetBytes(4), 0));
        }

        static public string ReadString(this Stream stream)
        {
            List<byte> lstBts = new List<byte>();
            for (int b = stream.ReadByte(); 0 < b && b <= 255 && lstBts.Count < OutilsRéseau.NB_OCTET_STR_MAX; b = stream.ReadByte())
                lstBts.Add((byte)b);
            return UTF8Encoding.UTF8.GetString(lstBts.ToArray());
        }

        static public int ReadInt(this Stream stream)
        {
            return BitConverter.ToInt32(stream.GetBytes(4), 0);
        }

        /*static public object ReadObject(this Stream stream, EType eType, IRessourcesDésérialiseur ressourcesDésérialiseur)
        {
            return ReadObject(stream, Element.ObtenirTypeElement(eType), ressourcesDésérialiseur);
        }*/

        static public IBinSerialisable[] ReadIBinParRéférence(this IRessourcesDésérialiseur ressourcesDésérialiseur, Stream stream)
        {
            ushort nbIBin = BitConverter.ToUInt16(stream.GetBytes(2), 0);
            IBinSerialisable[] tabElms = new IBinSerialisable[nbIBin];
            for (ushort i = 0; i < nbIBin; ++i)
                tabElms[i] = ressourcesDésérialiseur.RetrouverObject(stream) as IBinSerialisable;
            return tabElms;
        }

        static public Element ReadElement(this Stream stream, IRessourcesDésérialiseur ressourcesDésérialiseur)
        {
            Type type = ObtenirTypeElement((EType)stream.ReadByte());
            if (type == null) return null;
            else return stream.ReadObject(type, ressourcesDésérialiseur) as Element;
        }

        static public object ReadObject(this Stream stream, Type type, IRessourcesDésérialiseur ressourcesDésérialiseur)
        {
            if (type == null) return null;
            if(type == typeof(IBinSerialisable) || type.FindInterfaces(TypeFilter.ReferenceEquals, typeof(IBinSerialisable)).Any())
            {
                int b = stream.ReadByte();
                if (b < 0) return null;
                Type elmT = ObtenirTypeElement((EType)b);
                if (elmT == null) return null;
                else if (elmT != type && type != typeof(Element) && type != typeof(object) && type != typeof(IBinSerialisable))
                    throw new Exception("Le type d'élément attendu diffère de celui désérialisé !");

                ConstructorInfo construct = elmT.GetConstructor(new Type[] { typeof(Stream), typeof(IRessourcesDésérialiseur) });
                if(construct != null) return construct.Invoke(new object[] { stream, ressourcesDésérialiseur });
                else
                {
                    construct = elmT.GetConstructor(new Type[] { typeof(Stream) });
                    if (construct != null) return construct.Invoke(new object[] { stream });
                    else throw new Exception("Le type " + elmT + " n'a pas de constructeur acceptant un flux !");
                }
            }
            else if (type.IsArray)
            {
                //Array...
                byte[] bts = stream.GetBytes(2);
                Type atype = type.GetElementType();
                int nbItm = (bts != null ? BitConverter.ToUInt16(bts, 0) : 0);
                Array tab = Array.CreateInstance(atype, nbItm);
                for (ushort i = 0; i < nbItm; ++i)
                {
                    object o = ReadObject(stream, atype, ressourcesDésérialiseur);
                    tab.SetValue(o, i);
                }
                return tab;
            }
            else if (type.IsGenericType)
            {
                if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
                {
                    //Liste
                    Type atype = type.GenericTypeArguments.First();
                    object lst = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    object[] prms = new object[1];
                    MethodInfo metAdd = type.GetMethod("Add");
                    for (object obj = ReadObject(stream, atype, ressourcesDésérialiseur); obj != null; obj = ReadObject(stream, atype, ressourcesDésérialiseur))
                    {
                        prms[0] = obj;
                        metAdd.Invoke(lst, prms);
                    }
                    return lst;
                }
                throw new NotImplementedException("Le type " + type + " n'est pas pris en cahrge.");
            }
            else return stream.ReadObject(type);
        }

        static public object ReadObject(this Stream stream, Type type)
        {
            if (type == null) return null;
            else if (type.IsArray)
            {
                //Array...
                byte[] bts = stream.GetBytes(2);
                Type atype = type.GetElementType();
                int nbItm = (bts != null ? BitConverter.ToUInt16(bts, 0) : 0);
                object tab = Array.CreateInstance(atype, nbItm);

                for (ushort i = 0; i < nbItm; ++i)
                {
                    object o = ReadObject(stream, atype);
                    (tab as Array).SetValue(o, i);
                }
                return tab;
            }
            else if (type.IsGenericType)
            {
                if (type.FullName.StartsWith("System.Collections.Generic.List`1"))
                {
                    //Liste
                    Type atype = type.GenericTypeArguments.First();
                    object lst = type.GetConstructor(new Type[0]).Invoke(new object[0]);
                    object[] prms = new object[1];
                    MethodInfo metAdd = type.GetMethod("Add");
                    for (object obj = ReadObject(stream, atype); obj != null; obj = ReadObject(stream, atype))
                    {
                        prms[0] = obj;
                        metAdd.Invoke(lst, prms);
                    }
                    return lst;
                }
                throw new NotImplementedException("Le type " + type + " n'est pas pris en cahrge.");
            }
            else if (stream.Position < stream.Length)
            {
                 if (type == typeof(BigInteger))//Il s'agit d'un hash256
                {
                    byte[] bts = new byte[32];
                    stream.Read(bts, 0, 31);
                    bts[31] = 0;
                    return new BigInteger(bts);
                }
                else if (type.IsEnum)
                {
                    object obj = stream.ReadObject(Enum.GetUnderlyingType(type));
                    return Enum.ToObject(type, obj);
                }
                else if (type == typeof(GeoCoord2D))
                    return stream.ReadGeoCoord2D();
                else if (type == typeof(Point))
                    return stream.ReadPoint();
                else if (type == typeof(PointF))
                    return stream.ReadPointF();
                else if (type == typeof(Int64))
                    return BitConverter.ToInt64(stream.GetBytes(8), 0);
                else if (type == typeof(Int32))
                    return BitConverter.ToInt32(stream.GetBytes(4), 0);
                else if (type == typeof(Int16))
                    return BitConverter.ToInt16(stream.GetBytes(2), 0);
                else if (type == typeof(UInt64))
                    return BitConverter.ToUInt64(stream.GetBytes(8), 0);
                else if (type == typeof(UInt32))
                    return BitConverter.ToUInt32(stream.GetBytes(4), 0);
                else if (type == typeof(UInt16))
                    return BitConverter.ToUInt16(stream.GetBytes(2), 0);
                else if (type == typeof(Char))
                    return BitConverter.ToChar(stream.GetBytes(2), 0);
                else if (type == typeof(Byte))
                    return stream.ReadByte();
                else if (type == typeof(SByte))
                    return (SByte)stream.ReadByte();
                else if (type == typeof(Boolean))
                    return BitConverter.ToBoolean(stream.GetBytes(1), 0);
                else if (type == typeof(Single))
                    return BitConverter.ToSingle(stream.GetBytes(4), 0);
                else if (type == typeof(Double))
                    return BitConverter.ToDouble(stream.GetBytes(8), 0);
                else if (type == typeof(String))
                    return stream.ReadString();
                else return null;
            }
            else return null;
        }

        static public object[] DécodeCommande(this Stream stream, IRessourcesDésérialiseur resscDes, params ParameterInfo[] paramInfo)
        {
            return paramInfo.Select(pi => stream.ReadObject(pi.ParameterType, resscDes)).ToArray();
        }

        static public object[] DécodeCommande(this Stream stream, IRessourcesDésérialiseur resscDes, params Type[] types)
        {
            return types.Select(t => stream.ReadObject(t, resscDes)).ToArray();
        }

        static public Image RécupérerImage(this IRessourcesDésérialiseur iress, Stream stream)
        {
            string sig = stream.ReadString();
            if (sig.Length < 28) return null;
            else if (iress != null && iress.BibImage != null)
                return iress.BibImage.RécupérerOuCréerImage(sig);
            else return BibliothèqueImage.InitImageVide(sig);
        }

        static public Model2_5D RécupérerModel(this IRessourcesDésérialiseur iress, Stream stream)
        {
            string sig = stream.ReadString();
            if (sig.Length < 28) return null;
            else if (iress != null && iress.BibModel != null)
                return iress.BibModel.RécupérerOuCréerModel(sig);
            else return Model2_5D.CréerModelVide(sig);
        }

        static public Pioche RetrouverPioche(this IRessourcesDésérialiseur iress, Stream stream)
        {
            object obj = iress.RetrouverObject(stream);
            if (obj is Pioche) return obj as Pioche;
            else if (obj is IBinSerialisable)
            {
                Pioche pch = new Pioche((obj as IBinSerialisable).ElmId);
                return pch;
            }
            else return null;
        }

        static public Défausse RetrouverDéfausse(this IRessourcesDésérialiseur iress, Stream stream)
        {
            object obj = iress.RetrouverObject(stream);
            if (obj is Défausse) return obj as Défausse;
            else if (obj is IBinSerialisable)
            {
                Défausse pch = new Défausse((obj as IBinSerialisable).ElmId);
                return pch;
            }
            else return null;
        }

        public enum EMessage : byte
        {
            Information,
            Attention,
            Erreur,
            ConnexionRéussie,
            IdentifiantRefusée,
            CréaSession,
            RefuSession,
            JoinSession,
            QuitSession,
            Déconnexion
        }

        static public Type ObtenirTypeElement(EType elmType)
        {
            byte b = (byte)elmType;
            if (1 <= b && b < ElmTypeToType.Length)
                return ElmTypeToType[b];
            else return null;
        }

        static private readonly Type[] ElmTypeToType = new Type[]
        {
            null,
            typeof(Element),
            typeof(Element2D),
            typeof(Element2D2F),
            typeof(Figurine),
            typeof(Groupe),
            typeof(Paquet),
            typeof(Pile),
            typeof(Pioche),
            typeof(Défausse), //9
            null,
            null,
            null,
            null,
            null,
            null,//15
            null,
            null,
            null,
            null,
            typeof(Joueur), //20
            typeof(ElementRéseau) //21
        };
    }
}
