namespace Outbox.Shared.Domain
{
    public class OutboxEvent
    {
        public int Id { get; set; }
        public string Event { get; set; }
        public string Data { get; set; }
    }
}
