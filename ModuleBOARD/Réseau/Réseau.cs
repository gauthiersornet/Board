using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace ModuleBOARD.Réseau
{
    public static class OutilsRéseau
    {
        public const int NB_OCTET_MAX = 20*1024*1024;
        public const int NB_OCTET_NOM_UTILISATEUR_MAX = 31;

        public const int NB_OCTET_NOM_SESSION_MAX = 3*31;

        public const int NB_OCTET_COMMANDE = 1;

        static public bool EstChaineSecurisée(this string str)
        {
            return str.All(c => char.IsSeparator(c) ||
                                char.IsPunctuation(c) ||
                                char.IsLetterOrDigit(c));
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

        static public BigInteger RandomBigInteger(int nbBytes, Random rnd = null)
        {
            if (rnd == null) rnd = new Random();
            byte[] bts = new byte[nbBytes];
            rnd.NextBytes(bts);
            return new BigInteger(bts);
        }

        static public BigInteger SecuredRandomBigInteger128(Random rnd = null)
        {
            if (rnd == null) rnd = new Random();
            BigInteger a = 0;
            while (a <= 1000 || Prime256Bits <= a) a = RandomBigInteger(16, rnd);
            BigInteger b = 0;
            while (b <= 1000) b = RandomBigInteger(16, rnd);
            return FastPowMod(a, b, Prime256Bits) % Max128Bits;
        }

        static public BigInteger SecuredRandomBigInteger256(Random rnd = null)
        {
            if (rnd == null) rnd = new Random();
            BigInteger a = 0;
            while (a <= 1000 || Prime256Bits <= a) a = RandomBigInteger(32, rnd);
            BigInteger b = 0;
            while (b <= 1000) b = RandomBigInteger(32, rnd);
            return FastPowMod(a, b, Prime256Bits);
        }

        static public BigInteger ChargerUBigInteger(byte[] bInt)
        {
            if (bInt != null && bInt.Length > 0)
            {
                if (bInt[bInt.Length - 1] == 0)
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

        static public byte[] ULongToBytes(this byte[] dst, ulong val, int idx = 0)
        {
            for (int i = 0; i < Marshal.SizeOf(val); ++i)
                dst[idx + i] = (byte)(((val) >> (i*8)) & 0xFF);
            return dst;
        }

        static public ulong BytesToULong(this byte[] src, int idx = 0)
        {
            ulong res = 0;
            for (int i = 0; i < Marshal.SizeOf(res); ++i)
                res |= ((ulong)src[idx + i]) << (i * 8);
            return res;
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
            int idx;
            for (idx = 0; idx < bytes.Length && bytes[idx] != 0; ++idx) ;//On cherche le dernier 0
            return UTF8Encoding.UTF8.GetString(bytes, 0, idx);
        }
    }
}
