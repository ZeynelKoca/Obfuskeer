using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Obfuscating_with_mono_cecil
{
    class Encryptor
    {
        public static Encryptor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Encryptor();

                return _instance;
            }
        }

        private static Encryptor _instance = null;

        private char CipherHelper(char ch, int key)
        {
            if (!char.IsLetter(ch))
                return ch;

            char offset = char.IsUpper(ch) ? 'A' : 'a';
            return (char)((((ch + key) - offset) % 26) + offset);
        }

        public string CaesarEncipher(string input, int key)
        {
            string output = string.Empty;

            foreach (char ch in input)
                output += CipherHelper(ch, key);

            return output;
        }


        private byte[] GetHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        public string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}
