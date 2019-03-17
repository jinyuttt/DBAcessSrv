using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NetCrypto.Encrypt
{
   
    public class RsaEncryptProvider:IDataEncrypt
    {
        private const int RsaKeySize = 2048;
        private const string publicKeyFileName = "RSA.Pub";
        private const string privateKeyFileName = "RSA.Private";
      //  private const string PubKeyXML = "<RSAKeyValue><Modulus>yuW8mDcb1+n/fIKqNaT3LQ3qsKNBg4GC7ZD2KXEJqMOyk5x8JOgwgg3mwnie1LfqryzYHSIJLjxR35WznjrCBT+p07IkitGCPY6JuNI/w1KmaoPueb8V/j8YvPQEs6UIXgj/PJdsw1xPgzIxZj9fyxnXOTqbIee4bTOkT28610yKjiq/90dGvWFRmFWPhjTlet02Dt4Qe0nrK/DMCw2dIIcBqrAJyQCMa8dKObbx0Q7+32X71MB3IyzCWZWou8xMBNAxbIYF3Yu6zjLmcBjWpLAAud3tHp72XJ27sNSfZNR1x4Liqo9NnjOivuRnxIxwCpexBh42Qsfx7JSm3aKeZQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
       // private const string PrivKeyXml = "<RSAKeyValue><Modulus>yuW8mDcb1+n/fIKqNaT3LQ3qsKNBg4GC7ZD2KXEJqMOyk5x8JOgwgg3mwnie1LfqryzYHSIJLjxR35WznjrCBT+p07IkitGCPY6JuNI/w1KmaoPueb8V/j8YvPQEs6UIXgj/PJdsw1xPgzIxZj9fyxnXOTqbIee4bTOkT28610yKjiq/90dGvWFRmFWPhjTlet02Dt4Qe0nrK/DMCw2dIIcBqrAJyQCMa8dKObbx0Q7+32X71MB3IyzCWZWou8xMBNAxbIYF3Yu6zjLmcBjWpLAAud3tHp72XJ27sNSfZNR1x4Liqo9NnjOivuRnxIxwCpexBh42Qsfx7JSm3aKeZQ==</Modulus><Exponent>AQAB</Exponent><P>5LPiWxYbwHe/i/IRKJ84B5PrSPdtPqn1Uj/FUu+4ZMpNw3UEyt0u73MLgXLjwCrx7A484cLS4el5z0eOBAnjw2d1Hm6E/jh8sH5zQHv7u1rFefUMYRMYXSqirTVlj226ccFRE9OZ3lKPuXrodappqNstjlprV1AMDxfLHA1aUXs=</P><Q>4x1cWbomvGv8JKlginM9GN0sTcM2BeO961dE2K/zBN5CnmW3um7PvDHyb+ntYYoOaW1lx8V/TB3X5w6ywsMiZhe5uXmqiSWaj8vGAyJ+NM+K2AgHDcEqLFUiyTJ/XkqV2k7iMcSLuRO738OECeC/bEu3HGoGGryJWuEC+fEvGZ8=</Q><DP>B0Bk5vp2es3RNwC/5ofV4PehuDiQMDJ3Yto+yXhsYlW/zXjCZCRLPrBpJvubmRZDgXaaG5Zv1VXv1NCyAhLGNAXtwr9CXEUyPu5jfSHxQ2mHZWyNre5LEXkum0tcIwYZqU214mkNMe1wPTNWd5SlsQLyGNdpG+Wf3EKm4AbUXE0=</DP><DQ>Ea8klLwA7iT+YiBqKv2kIT5/h6KOn1DHZf7KlpDEvHlN+KV08+hS9pVxCjPNzw1/58ej6DVBnzynpg8n7jBhik+In5+QntM1wMKeLXpPF2+doQqm+fQzg3Yxmjb7Ye0u0+vWgweJ1aRquZawvlAot5cBsA21YfmSPGhO4gVcpIM=</DQ><InverseQ>bSssYm1rAcXrZ3G9gDki8Qj0HUUXRrjNJCK2QTHhU5cGY5yiyQE+JVkHOkteKDEaGhaMbGj9cn4D6x22FVV6F9zx+L1RFfrtJpdD9/iYKll6nD3HzpSQ+3AoSE8R8e6bQEWlioW8dICm4A1Acaj7kJyJw1JpJKfjnRrsr05dnQI=</InverseQ><D>KB+GTBOZzfjYLScpwbH9r0sxPf0K15ak7ZXdGBTidB0/EzG+2w2Piih1mb+AqVA1eK7Fjf1NE3eaOTzBaGj2NVOBoft4fnsv5jxpv8LUGSwe/LFaV3kSQFT572PSCjR4kx/0WWcYewmmL6udWTrvFprllMuiIfJQ5kdwFsVIPYrrN7D2A6FOyVuXmmr1DW6+6E/MUjvOWA2UBf4VeybQRsjekaD2ckIM0UK/7+8CWIoNtUzK2ZJ1oqyOk5oVk8Ja0VI3AZSZL1s5Xx/estVZfpmtVGg20T21yXZ7PREpcZQxK67ywNFm12dreZw8sByVvLGKazJfGtijSfHadEPngQ==</D></RSAKeyValue>";
        private RSAKey rSAKey;

        /// <summary>
        ///在给定路径中生成XML格式的私钥和公钥。
        /// </summary>
        public void GenerateKeys(string path)
        {
            if (!File.Exists(Path.Combine(path, publicKeyFileName))||!File.Exists(Path.Combine(path, privateKeyFileName)))
                { 
                using (var rsa = new RSACryptoServiceProvider(RsaKeySize))
                {
                    try
                    {
                        // 获取私钥和公钥。
                        var publicKey = rsa.ToXmlString(false);
                        var privateKey = rsa.ToXmlString(true);
                        // 保存到磁盘
                        if (!File.Exists(Path.Combine(path, publicKeyFileName)))
                        {
                            File.WriteAllText(Path.Combine(path, publicKeyFileName), publicKey);
                        }
                        if (!File.Exists(Path.Combine(path, privateKeyFileName)))
                        {
                            File.WriteAllText(Path.Combine(path, privateKeyFileName), privateKey);
                        }
                      //  rSAKey.PrivateKey = privateKey;
                      //  rSAKey.PublicKey = publicKey;
                        //Console.WriteLine(string.Format("生成的RSA密钥的路径: {0}\\ [{1}, {2}]", path, publicKeyFileName, privateKeyFileName));
                    }
                    finally
                    {
                        rsa.PersistKeyInCsp = false;
                    }
                }
            }
        }

        public string Encrypt(string msg, string key = null)
        {
            try
            {
                byte[] PlainTextBArray;
                byte[] CypherTextBArray;
                string Result;
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(RsaKeySize);
                rsa.ImportCspBlob(Convert.FromBase64String(key));
                PlainTextBArray = Encoding.UTF8.GetBytes(msg);
                CypherTextBArray = rsa.Encrypt(PlainTextBArray, false);
                Result = Convert.ToBase64String(CypherTextBArray);
                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] Encrypt(byte[] msg, string key = null)
        {
            try
            {
                byte[] CypherTextBArray;
                byte[] Result;
                RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(RsaKeySize);
                rsa.ImportCspBlob(Convert.FromBase64String(key));
                CypherTextBArray = rsa.Encrypt(msg, false);
                Result = CypherTextBArray;
                return Result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] Encrypt(Stream msg, string key = null)
        {
            byte[] buf = new byte[msg.Length-msg.Position];
            msg.Read(buf, 0, buf.Length);
            // return Encrypt(buf, key);
            return null;
        }
    }
}
