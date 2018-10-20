using System;

namespace bnetlauncher.Utils
{
    internal class MessageBox: System.Windows.Forms.MessageBox
    {
        /// <summary>
        /// Enumeration for the types of Message ShowMessageAndExit can show.
        /// </summary>
        public enum MessageType { Error, Warning, Info};

         /// <summary>
        /// Releases the mutex and shows an error message to the user, closing the mutex and exiting on okay.
        /// Note: This method will also call CloseBnetClientIfLast()
        /// </summary>
        /// <param name="message">Error message to show.</param>
        /// <param name="title">Title of the error to show. (optional)</param>
        /// <param name="type">Type of message to show, changes title suffix and icon. Defaults to Error</param>
        /// <param name="exit_code">Exit code, defaults to -1 (optional)</param>
        public static void ShowAndExit(string message, string title = "",
            MessageType type = MessageType.Error, int exit_code = -1)
        {
            // Select the type of icon and suffix to add to the message
            MessageBoxIcon icon;
            string suffix;
            switch (type)
            {
                case MessageType.Info:
                    icon = MessageBoxIcon.Information;
                    suffix = "Info: ";
                    break;

                case MessageType.Warning:
                    icon = MessageBoxIcon.Warning;
                    suffix = "Warning: ";
                    break;

                default:
                    icon = MessageBoxIcon.Error;
                    suffix = "Error: ";
                    break;
            }

            try
            {
                // We hit an error, so we let the next bnetlauncher instant have a go while we show an error
                if (launcher_mutex != null) launcher_mutex.ReleaseMutex();

                // Shows the actual message
                System.Windows.Forms.MessageBox.Show(message, suffix + title, MessageBoxButtons.OK, icon);

                // Cleans up, makes sure the battle.net client isn't left running under steam or
                // the mutex is abandoned.

                // Did we start the battle.net launcher?
                CloseClientIfLast();

                // Cleans up the mutex
                if (launcher_mutex != null) launcher_mutex.Close();
            }
            catch (Exception ex)
            {
                // ignore the two possible Exceptions
                // ApplicationException - The calling thread does not own the mutex.
                // ObjectDisposedException - The current instance has already been disposed.
                Logger.Error("Error releasing the mutex.", ex);
            }

            // calls the end of the application
            Logger.Information($"Exiting.");
            Environment.Exit(exit_code);
        }
    }
}