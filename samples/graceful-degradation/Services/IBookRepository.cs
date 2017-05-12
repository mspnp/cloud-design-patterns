using System.Threading.Tasks;
using ResiliencyDemos.Models;

namespace ResiliencyDemos.Services
{
    public interface IBookRepository
    {
        Task<Book> GetBookAsync(int id);
    }
}
