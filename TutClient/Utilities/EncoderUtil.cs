using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

using TutClient.Handlers;

namespace TutClient.Utilities
{
    public interface IEncoderUtil
    {
        Encoding Encoder { get; }

        string GetString(byte[] data);
        byte[] GetBytes(string data);
        int GetPythonLength(string data);
    }

    public class EncoderUtil : IEncoderUtil
    {
        private readonly Encoding _encoder;
        public Encoding Encoder => _encoder;
        
        public EncoderUtil()
        {
            _encoder = ApplicationSettings.IsLinuxServer ? Encoding.UTF8 : Encoding.Unicode;
        }

        public string GetString(byte[] data)
        {
            return _encoder.GetString(data);
        }

        public byte[] GetBytes(string data)
        {
            return _encoder.GetBytes(data);
        }

        /// <summary>
        /// Get the length of byte data in utf8
        /// </summary>
        /// <param name="data">The data to get the length of</param>
        /// <returns>The length of the data in utf8 bytes</returns>
        public int GetPythonLength(string data)
        {
            return data.Sum(t => Encoding.UTF8.GetByteCount(t.ToString()));
        }

        
    }
}