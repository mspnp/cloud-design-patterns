using System.Threading;
using System.Threading.Tasks;

namespace ClientConsumer
{
    public interface IConsumer
    {
        void Configure();
        Task ProcessMessages(CancellationToken token);
    }
}