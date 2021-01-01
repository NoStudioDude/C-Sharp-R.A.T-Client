using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using TutClient.Enum;

namespace TutClient.Helpers
{
    public interface IEncryptionHelper
    {
        string Encrypt(string clearText);
        string Decrypt(string cipherText);
    }

    public class EncryptionHelper : IEncryptionHelper
    {
        const string ENCRYPTION_KEY = "MAKV2SPBNI99212"; //this is the secret encryption key you want to hide dont show it to other guys

        private readonly IReportHelper _reportHelper;

        public EncryptionHelper(IReportHelper reportHelper)
        {
            _reportHelper = reportHelper;
        }

        /// <summary>
        /// Encrypt data
        /// </summary>
        /// <param name="clearText">The message to encrypt</param>
        /// <returns>The encrypted Base64 CipherText</returns>
        public string Encrypt(string clearText)
        {
            try
            {
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText); //Bytes of the message
                using (Aes encryptor = Aes.Create()) //Create a new AES decryptor
                {
                    //Encrypt the data
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(ENCRYPTION_KEY, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        clearText = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return clearText; //Return the encrypted text
            }
            catch (Exception) //Something went wrong
            {
                _reportHelper.ReportError(ErrorType.ENCRYPT_DATA_CORRUPTED, "Can't encrypt message!", "Message encryption failed!"); //Report error to server
                return clearText; //Send the plain text data
            }
        }

        /// <summary>
        /// Decrypt encrypted data
        /// </summary>
        /// <param name="cipherText">The data to decrypt</param>
        /// <returns>The plain text message</returns>
        public string Decrypt(string cipherText)
        {
            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText); //Get the encrypted message's bytes
                using (var encryptor = Aes.Create()) //Create a new AES object
                {
                    //Decrypt the text
                    var pdb = new Rfc2898DeriveBytes(ENCRYPTION_KEY, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (var ms = new MemoryStream())
                    {
                        using (var cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText; //Return the plain text data
            }
            catch (Exception ex) //Something went wrong
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Cipher Text: " + cipherText);
                _reportHelper.ReportError(ErrorType.DECRYPT_DATA_CORRUPTED, "Can't decrypt message!", "Message decryption failed!"); //Report error to the server
                return "error"; //Return error
            }
        }
    }
}