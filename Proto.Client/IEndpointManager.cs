namespace Proto.Client
{
    public interface IEndpointManager
    {
        PID? GetEndpoint(PID destination);
        void Start();
    }
}