namespace Service.Messaging
{
    public interface IOutcomeSender
    {
        void Send(string status, string fileId, string replyTo);
    }
}
