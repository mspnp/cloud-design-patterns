
namespace Pnp.Samples.ClaimCheckPattern
{
    public interface ISampleKafkaProducer
    {
        Task SendMessageAsync(string message);
    }
}