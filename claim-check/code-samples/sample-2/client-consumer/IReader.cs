using System.Threading;
using System.Threading.Tasks;

namespace ClientConsumer
{
    public interface IReader
    {
        void Configure();
        Task ProcessMessages(CancellationToken token);
    }
}