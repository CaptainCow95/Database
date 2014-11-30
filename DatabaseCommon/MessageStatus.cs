namespace Database.Common
{
    public enum MessageStatus
    {
        Created,
        Sending,
        Sent,
        SendingFailure,
        WaitingForResponse,
        ResponseReceived,
        ResponseTimeout,
        Received
    }
}