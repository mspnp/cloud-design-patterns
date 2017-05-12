using System.Threading.Tasks;
using ResiliencyDemos.Models;

namespace ResiliencyDemos.Services
{
    public class BookRepository : IBookRepository
    {
        public Task<Book> GetBookAsync(int id)
        {
            // Simulate returning a book from a database.
            return Task.FromResult(new Book
            {
                Id = id,
                Title = "Cloud Design Patterns: Prescriptive Architecture Guidance for Cloud Applications",
                ShortTitle = "Cloud Design Patterns",
                Author = "Microsoft patterns & practices",
                Summary =
                    "Cloud applications have a unique set of characteristics. They run on commodity hardware, provide services to untrusted users, and deal with unpredictable workloads. These factors impose a range of problems that you, as a designer or developer, need to resolve. Your applications must be resilient so that they can recover from failures, secure to protect services from malicious attacks, and elastic in order to respond to an ever changing workload.\n" +
                    "This guide demonstrates design patterns that can help you to solve the problems you might encounter in many different areas of cloud application development. Each pattern discusses design considerations, and explains how you can implement it using the features of Windows Azure. The patterns are grouped into categories: availability, data management, design and implementation, messaging, performance and scalability, resilience, management and monitoring, and security.\n" +
                    "You will also see more general guidance related to these areas of concern. It explains key concepts such as data consistency and asynchronous messaging. In addition, there is useful guidance and explanation of the key considerations for designing features such as data partitioning, telemetry, and hosting in multiple datacenters.\n" +
                    "These patterns and guidance can help you to improve the quality of applications and services you create, and make the development process more efficient. Enjoy!",
                ImageUri = "https://resiliencydemosdata01.blob.core.windows.net/images/cloud-design-patterns-cover.jpg"
            });
        }
    }
}
