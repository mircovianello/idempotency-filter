namespace idempotency_filter.Idempotency;
public class IdempotentResponse
{
    public IdempotentResponse(string key, string connectionId)
    {
        Key = key;
        ConnectionId = connectionId;
    }

    public string Key { get; set; }
    public string ConnectionId { get; set; }
    public bool Finished { get; set; }
    public int? StatusCode { get; set; }
    public string Body { get; set; }
}