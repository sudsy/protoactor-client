namespace Proto.Client
{
    public interface IEndpointManager
    {
        PID? GetEndpoint(string address);
        void Start();
    }
}