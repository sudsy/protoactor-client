using System;
using System.Threading.Tasks;
using Proto.Client.TestMessages;
using Proto.Remote;

namespace Proto.Client.TestNode
{
    class Program
    {
        static void Main(string[] args)
        {

            Remote.Remote.Start("localhost", 44000);
            ClientHost.Start("localhost", 55000);

            var props = Props.FromProducer(() => new EchoActor());
            Remote.Remote.RegisterKnownKind("EchoActor", props);
            
            Console.ReadLine();
            
        }
    }


     public class EchoActor : IActor
    {
     

        public EchoActor()
        {
           
        }

        public Task ReceiveAsync(IContext context)
        {
            
            switch (context.Message)
            {
                case Ping ping:
                    try
                    {
                        context.Respond(new Pong {Message = $"{context.Self.Address} {ping.Message}"});
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                   
                    return Actor.Done;
                default:
                    return Actor.Done;
            }
        }
    }
}
