namespace Database.Common
{
    /// <summary>
    /// Represents the different connection statuses.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// The connection has been made, but is being validated.
        /// </summary>
        ConfirmingConnection,

        /// <summary>
        /// The connection has successfully been established.
        /// </summary>
        Connected
    }
}