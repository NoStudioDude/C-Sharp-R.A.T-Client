using System;
using System.IO;

using TutClient.Enum;
using TutClient.Helpers;

namespace TutClient.Controls
{
    public interface IDownloadUpload
    {
        event EventHandler<string> SendCommand;
        event EventHandler<byte[]> SendByte;

        bool IsFileDownload { get; set; }
        byte[] RecvFile { get; }
        int FupSize { get; }
        string FupLocation { get; }

        void DownloadFile(string text);
        void UploadFile(string text);
        void FileReceivedConfirmationOnServer();
    }

    public class DownloadUpload : IDownloadUpload
    {
        public event EventHandler<string> SendCommand;
        public event EventHandler<byte[]> SendByte;

        public bool IsFileDownload
        {
            get => _isFileDownload;
            set => _isFileDownload = value;
        }

        public byte[] RecvFile => _recvFile;
        public int FupSize => _fupSize;
        public string FupLocation => _fupLocation;

        private readonly IReportHelper _reportHelper;

        private string _fupLocation;
        private int _fupSize;
        private bool _isFileDownload;
        private byte[] _recvFile;
        private string _fdlLocation;

        public DownloadUpload(IReportHelper reportHelper)
        {
            _reportHelper = reportHelper;
        }

        public void UploadFile(string text)
        {
            _fupLocation = text.Split('§')[1]; //Get the location of the new file

            if (File.Exists(_fupLocation)) //Check if the file already exists
            {
                _reportHelper.ReportError(ErrorType.FILE_EXISTS, "Can't upload file!", "Manager detected that this file exists!"); //Report error to the server
                return;
            }
            _fupSize = int.Parse(text.Split('§')[2]); //Get the size of the file
            _isFileDownload = true; //Set the socket to file download mode
            _recvFile = new byte[_fupSize]; //Create a new buffer for the file

            SendCommand?.Invoke(this, "fconfirm"); //Confirm to start streaming the file
        }
        
        public void DownloadFile(string text)
        {
            _fdlLocation = text.Substring(4); //The file the server wants to download
            if (!File.Exists(_fdlLocation)) //File doesn't exist
            {
                _reportHelper.ReportError(ErrorType.FILE_NOT_FOUND, "Can't download file!", "Manager is unable to locate: " + _fdlLocation); //Report error to the server
                return;
            }

            // TODO: rework file sending algorithm, send in chunks
            var size = ApplicationSettings.IsLinuxServer
                ? Convert.ToBase64String(File.ReadAllBytes(_fdlLocation)).Length.ToString()
                : new FileInfo(_fdlLocation).Length.ToString();

            SendCommand?.Invoke(this,"finfo§" + size); //Send the file's size to the server
        }

        public void FileReceivedConfirmationOnServer()
        {
            var sendFile = File.ReadAllBytes(_fdlLocation); //Read the bytes of the file
            if (ApplicationSettings.IsLinuxServer)
            {
                SendCommand?.Invoke(this, $"filestr{Convert.ToBase64String(sendFile)}");
            }
            else
            {
                SendByte?.Invoke(this, sendFile);
            }
        }
    }
}
