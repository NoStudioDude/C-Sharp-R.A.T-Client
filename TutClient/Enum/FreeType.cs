namespace TutClient.Enum
{
    /// <summary>
    /// Free memory allocation options
    /// </summary>
    public enum FreeType
    {
        /// <summary>
        /// Recommit memory
        /// </summary>
        Decommit = 0x4000,
        /// <summary>
        /// Release memory
        /// </summary>
        Release = 0x8000,
    }
}
