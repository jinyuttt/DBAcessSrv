#region << 版 本 注 释 >>
/*----------------------------------------------------------------
* 项目名称 ：NetCrypto.RSACrypto
* 项目描述 ：
* 类 名 称 ：RSAParametersKey
* 类 描 述 ：
* 命名空间 ：NetCrypto.RSACrypto
* CLR 版本 ：4.0.30319.42000
* 作    者 ：jinyu
* 创建时间 ：2019/3/8 1:03:26
* 版 本 号 ：v1.0.0.0
*******************************************************************
* Copyright @ jinyu 2019. All rights reserved.
*******************************************************************
//----------------------------------------------------------------*/
#endregion


using Serializer;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

namespace NetCrypto.RSACrypto
{
    /* ============================================================================== 
* 功能描述：RSAParametersKey 钥为128位或196位，IV为64位
* 创 建 者：jinyu 
* 创建日期：2019 
* 更新时间 ：2019
* ==============================================================================*/
    public class RSAParametersKey
    {
        private const string defName = "RSA";
        private const int RsaKeySize = 2048;
        private const string publicKeyFileName = "PRSA.Pub";
        private const string privateKeyFileName = "PRSA.Private";

        /// <summary>
        /// xml格式的秘钥转换
        /// </summary>
        /// <param name="rsa"></param>
        /// <param name="xmlString"></param>
        public static void FromXmlString(RSA rsa, string xmlString)
        {
            RSAParameters parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
             xmlDoc.LoadXml(xmlString);
          
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Exponent": parameters.Exponent = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "P": parameters.P = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Q": parameters.Q = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DP": parameters.DP = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DQ": parameters.DQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "InverseQ": parameters.InverseQ = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "D": parameters.D = (string.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        /// <summary>
        /// 按照补位规则计算大小
        /// </summary>
        /// <param name="rsa"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
       public   static int GetMaxBlockSize(RSA rsa, RSAEncryptionPadding padding)
        {
            var offset = 0;
            if (padding.Mode == RSAEncryptionPaddingMode.Pkcs1)
            {
                offset = 11;
            }
            else
            {
                if (padding.Equals(RSAEncryptionPadding.OaepSHA1))
                {
                    offset = 42;
                }

                if (padding.Equals(RSAEncryptionPadding.OaepSHA256))
                {
                    offset = 66;
                }

                if (padding.Equals(RSAEncryptionPadding.OaepSHA384))
                {
                    offset = 98;
                }

                if (padding.Equals(RSAEncryptionPadding.OaepSHA512))
                {
                    offset = 130;
                }
            }
            return rsa.KeySize / 8 - offset;
        }

        /// <summary>
        /// 从证书中获取公钥
        /// </summary>
        /// <param name="cerPath"></param>
        /// <returns></returns>
        private static  string GetPublicKeyFromCer(string cerPath)
        {
            X509Certificate2 pubcrt = new X509Certificate2(cerPath);
            RSA pubkey = (RSA)pubcrt.PublicKey.Key;

            var p= pubkey.ExportParameters(false);
            return ConvertToPublicXML(p);
        }

        /// <summary>
        /// 转换公钥xml
        /// </summary>
        /// <param name="rSA"></param>
        /// <returns></returns>
        private static string ConvertToPublicXML(RSAParameters rSA)
        {
            string xml = "<RSAKeyValue>< Modulus ></ Modulus ><Exponent></ Exponent > </ RSAKeyValue > ";
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
           var lst= document.GetElementsByTagName("Modulus");
            if(lst.Count>0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.Modulus);
            }
            lst = document.GetElementsByTagName("Exponent");
            if(lst.Count>0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.Exponent);
            }
            return document.InnerXml;
        }

        /// <summary>
        /// 转换私钥xml
        /// </summary>
        /// <param name="rSA"></param>
        /// <returns></returns>
        private static string ConvertToPrivateXML(RSAParameters rSA)
        {
            string xml = "<RSAKeyValue><Modulus></Modulus><Exponent></Exponent><P></P>"+
                          "<Q></Q><DP></DP><DQ></DQ><InverseQ></InverseQ><D></D></RSAKeyValue >";
            XmlDocument document = new XmlDocument();
            document.LoadXml(xml);
            var lst = document.GetElementsByTagName("Modulus");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.Modulus);
            }
            lst = document.GetElementsByTagName("Exponent");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.Exponent);
            }
            lst = document.GetElementsByTagName("P");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.P);
            }
            lst = document.GetElementsByTagName("Q");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.Q);
            }
            lst = document.GetElementsByTagName("DP");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.DP);
            }
            lst = document.GetElementsByTagName("DQ");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.DQ);
            }
            lst = document.GetElementsByTagName("InverseQ");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.InverseQ);
            }
            lst = document.GetElementsByTagName("D");
            if (lst.Count > 0)
            {
                lst.Item(0).InnerText = Convert.ToBase64String(rSA.D);
            }
            return document.InnerXml;
        }

        private static RSAParameters DecodeRSAPrivateKey(byte[] privkey)
        {
            byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;
            MemoryStream mem = new MemoryStream(privkey);
            BinaryReader binr = new BinaryReader(mem);
            RSAParameters rSA = new RSAParameters();
            byte bt = 0;
            ushort twobytes = 0;
            int elems = 0;
            try
            {
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)
                    binr.ReadByte();
                else if (twobytes == 0x8230)
                    binr.ReadInt16();
                else
                    return rSA;

                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)
                    return rSA;
                bt = binr.ReadByte();
                if (bt != 0x00)
                    return rSA;

                elems = GetIntegerSize(binr);
                MODULUS = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                E = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                D = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                P = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                Q = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                DP = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                DQ = binr.ReadBytes(elems);

                elems = GetIntegerSize(binr);
                IQ = binr.ReadBytes(elems);

              
                RSAParameters RSAparams = new RSAParameters();
                RSAparams.Modulus = MODULUS;
                RSAparams.Exponent = E;
                RSAparams.D = D;
                RSAparams.P = P;
                RSAparams.Q = Q;
                RSAparams.DP = DP;
                RSAparams.DQ = DQ;
                RSAparams.InverseQ = IQ;
                
                return RSAparams;
            }
            catch (Exception)
            {
                return  rSA;
            }
            finally { binr.Close(); }
        }

        public static RSA CreateRsaFromPublicKey(string publicKeyString)
        {
            byte[] SeqOID = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            byte[] x509key;
            byte[] seq = new byte[15];
            int x509size;

            x509key = Convert.FromBase64String(publicKeyString);
            x509size = x509key.Length;

            using (var mem = new MemoryStream(x509key))
            {
                using (var binr = new BinaryReader(mem))
                {
                    byte bt = 0;
                    ushort twobytes = 0;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130)
                        binr.ReadByte();
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();
                    else
                        return null;

                    seq = binr.ReadBytes(15);
                  

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8103)
                        binr.ReadByte();
                    else if (twobytes == 0x8203)
                        binr.ReadInt16();
                    else
                        return null;

                    bt = binr.ReadByte();
                    if (bt != 0x00)
                        return null;

                    twobytes = binr.ReadUInt16();
                    if (twobytes == 0x8130)
                        binr.ReadByte();
                    else if (twobytes == 0x8230)
                        binr.ReadInt16();
                    else
                        return null;

                    twobytes = binr.ReadUInt16();
                    byte lowbyte = 0x00;
                    byte highbyte = 0x00;

                    if (twobytes == 0x8102)
                        lowbyte = binr.ReadByte();
                    else if (twobytes == 0x8202)
                    {
                        highbyte = binr.ReadByte();
                        lowbyte = binr.ReadByte();
                    }
                    else
                        return null;
                    byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                    int modsize = BitConverter.ToInt32(modint, 0);

                    int firstbyte = binr.PeekChar();
                    if (firstbyte == 0x00)
                    {
                        binr.ReadByte();
                        modsize -= 1;
                    }

                    byte[] modulus = binr.ReadBytes(modsize);

                    if (binr.ReadByte() != 0x02)
                        return null;
                    int expbytes = (int)binr.ReadByte();
                    byte[] exponent = binr.ReadBytes(expbytes);

                    var rsa = RSA.Create();
                    var rsaKeyInfo = new RSAParameters
                    {
                        Modulus = modulus,
                        Exponent = exponent
                    };
                    rsa.ImportParameters(rsaKeyInfo);
                    return rsa;
                }

            }
        }

        private static int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            byte lowbyte = 0x00;
            byte highbyte = 0x00;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)
                return 0;
            bt = binr.ReadByte();

            if (bt == 0x81)
                count = binr.ReadByte();
            else
                if (bt == 0x82)
            {
                highbyte = binr.ReadByte();
                lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;
            }
            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        public static byte[] StructToBytes(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structObj, buffer, false);
                byte[] bytes = new byte[size];
                Marshal.Copy(buffer, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        //byte[]转换为struct
        public static T BytesToStruct<T>(byte[] bytes)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return Marshal.PtrToStructure<T>(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// 设置key
        /// </summary>
        /// <param name="rsa"></param>
        /// <param name="key"></param>
        public static void SetRSAKeys(RSA rsa, string key)
        {
            if (IsXml(key))
            {
                FromXmlString(rsa, key);
            }
            else
            {
                FromRSAParameters(rsa, key);
            }
        }

        public static void CreateKeys(string path)
        {
            using (var rsa = RSA.Create())
            {
                rsa.KeySize = RsaKeySize;
                var publicKey = rsa.ExportParameters(false);
                var privateKey = rsa.ExportParameters(true);
                //
                //  byte[] pubKey = StructToBytes(publicKey);
                //  byte[] priKey = StructToBytes(privateKey);
                byte[] pubKey = SerializerFactory<CommonSerializer>.Serializer(publicKey);
                byte[] priKey = SerializerFactory<CommonSerializer>.Serializer(privateKey);
                if (!File.Exists(Path.Combine(path, publicKeyFileName)))
                {
                    File.WriteAllBytes(Path.Combine(path, publicKeyFileName), pubKey);
                }
                if (!File.Exists(Path.Combine(path, privateKeyFileName)))
                {
                    File.WriteAllBytes(Path.Combine(path, privateKeyFileName), priKey);
                }
            }
        }

        /// <summary>
        /// 获取秘钥
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static RSAKey GetRSAKey(string path)
        {
            RSAKey key = new RSAKey();
            if (File.Exists(Path.Combine(path, publicKeyFileName)))
            {
                key.PublicKey = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(path, publicKeyFileName)));
            }
            if (File.Exists(Path.Combine(path, privateKeyFileName)))
            {
                key.PrivateKey = Convert.ToBase64String(File.ReadAllBytes(Path.Combine(path, privateKeyFileName)));
            }
            return key;
        }

        private static bool IsXml(string key)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(key);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
      
        /// <summary>
        /// 通过导出RSAParameters值转换
        /// </summary>
        /// <param name="rsa"></param>
        /// <param name="param"></param>
        public static void FromRSAParameters(RSA rsa, string param)
        {
            byte[] p = Convert.FromBase64String(param);
            RSAParameters reslut = SerializerFactory<CommonSerializer>.Deserialize<RSAParameters>(p);
          
          //  RSAParameters reslut = BytesToStruct<RSAParameters>(p);
            rsa.ImportParameters(reslut);

        }

        ///// <summary>
        ///// 将C#格式公钥转成Java格式公钥
        ///// </summary>
        ///// <param name="publicKey"></param>
        ///// <returns></returns>
        //public static RsaKeyParameters RSAPublicKeyDotNet2Java(string publicKey)
        //{
        //    XmlDocument doc = new XmlDocument();
        //    doc.LoadXml(publicKey);
        //    BigInteger m = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Modulus")[0].InnerText));
        //    BigInteger p = new BigInteger(1, Convert.FromBase64String(doc.DocumentElement.GetElementsByTagName("Exponent")[0].InnerText));
        //    RSAParameters pub = new RSAParameters(m, p);

        //    return pub;
        //}
    }
}
