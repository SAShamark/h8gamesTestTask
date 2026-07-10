using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Services.Storage.DataProtection
{
    public static class DataProtectionManager
    {
#if UNITY_EDITOR
        private static readonly bool EnableProtection = false;
#else
        private static readonly bool EnableProtection = true;
#endif
        private static readonly string DeviceID = SystemInfo.deviceUniqueIdentifier;

        public static string Encode(string data, string key)
        {
            if (EnableProtection)
            {
                string keytext = string.Concat(DeviceID, " ", key);
                byte[] hashBytes = ComputeSha256Hash(keytext);
                data = Encrypt(data, hashBytes);
            }

            return data;
        }

        public static string Decode(string data, string key)
        {
            if (EnableProtection)
            {
                string keytext = string.Concat(DeviceID, " ", key);
                byte[] hashBytes = ComputeSha256Hash(keytext);
                data = Decrypt(data, hashBytes);
            }

            return data;
        }

        private static string Encrypt(string data, byte[] keyBytes)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.GenerateIV();
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    using (StreamWriter sw = new StreamWriter(cs))
                    {
                        sw.Write(data);
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        private static string Decrypt(string data, byte[] keyBytes)
        {
            byte[] cipherData = Convert.FromBase64String(data);

            using (Aes aes = Aes.Create())
            {
                byte[] iv = new byte[aes.BlockSize / 8];
                byte[] cipherBytes = new byte[cipherData.Length - iv.Length];

                Array.Copy(cipherData, iv, iv.Length);
                Array.Copy(cipherData, iv.Length, cipherBytes, 0, cipherBytes.Length);

                aes.Key = keyBytes;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream(cipherBytes))
                using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (StreamReader sr = new StreamReader(cs))
                {
                    return sr.ReadToEnd();
                }
            }
        }

        private static byte[] ComputeSha256Hash(string data)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        }
    }
}