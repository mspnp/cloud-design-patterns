namespace ResiliencyDemos.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public string Author { get; set; }
        public string Summary { get; set; }
        public string ImageUri { get; set; }
    }
}