using System;

namespace TutClient.Enum
{
    /// <summary>
    /// Mouse Event Codes
    /// </summary>
    [Flags]
    public enum MouseEventFlags
    {
        /// <summary>
        /// Press down the left click
        /// </summary>
        LEFTDOWN = 0x00000002,
        /// <summary>
        /// Release the left click
        /// </summary>
        LEFTUP = 0x00000004,
        /// <summary>
        /// Press down the middle (scroll) button
        /// </summary>
        MIDDLEDOWN = 0x00000020,
        /// <summary>
        /// Release the middle (scroll) button
        /// </summary>
        MIDDLEUP = 0x00000040,
        /// <summary>
        /// Move the mouse
        /// </summary>
        MOVE = 0x00000001,
        /// <summary>
        /// ?
        /// </summary>
        ABSOLUTE = 0x00008000,
        /// <summary>
        /// Press down the right click
        /// </summary>
        RIGHTDOWN = 0x00000008,
        /// <summary>
        /// Release the right click
        /// </summary>
        RIGHTUP = 0x00000010
    }
}