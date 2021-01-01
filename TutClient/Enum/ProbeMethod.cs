namespace TutClient.Enum
{
    /// <summary>
    /// The methods to use when probing statup
    /// </summary>
    public enum ProbeMethod
    {
        /// <summary>
        /// Use the startup folder
        /// </summary>
        StartUpFolder,
        /// <summary>
        /// Use the registry
        /// </summary>
        Registry,
        /// <summary>
        /// Use the TaskScheduler
        /// </summary>
        TaskScheduler
    }
}
