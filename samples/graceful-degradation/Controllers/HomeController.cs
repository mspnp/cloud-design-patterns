using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using ResiliencyDemos.Services;


namespace ResiliencyDemos.Controllers
{
    public class HomeController : Controller
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private readonly IBookRepository _bookRepository;

        public HomeController(IBookRepository bookRepository)
        {
            _bookRepository = bookRepository;
        }

        public async Task<ActionResult> Index(string mode = null)
        {
            var book = await _bookRepository.GetBookAsync(1);

            return View(mode ?? "Unprotected", book);
        }

        public async Task<ActionResult> DelayedImage(string uri)
        {
            // simulate delay for demo purposes
            await Task.Delay(3000);

            // fetch and forward the image
            var fileName = Path.GetFileName(new Uri(uri).AbsolutePath);
            var contentType = MimeMapping.GetMimeMapping(fileName);
            var stream = await HttpClient.GetStreamAsync(uri);
            return File(stream, contentType, fileName);
        }
    }
}