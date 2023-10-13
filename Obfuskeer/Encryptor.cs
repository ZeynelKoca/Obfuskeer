using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Obfuskeer
{
    class Encryptor
    {
        private static Encryptor _instance;
        public static Encryptor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Encryptor();
                }
                return _instance;
            }
        }

        private char CipherHelper(char ch, int key)
        {
            if (!char.IsLetter(ch))
                return ch;

            char offset = char.IsUpper(ch) ? 'A' : 'a';
            return (char)((((ch + key) - offset) % 26) + offset);
        }

        public string CaesarEncipher(string input, int key)
        {
            return input.Aggregate(string.Empty, (current, ch) => current + CipherHelper(ch, key));
        }

        public string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private byte[] GetHash(string inputString)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        public string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
