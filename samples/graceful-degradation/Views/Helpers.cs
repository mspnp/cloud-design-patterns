using System.Web;
using System.Web.Mvc;

namespace ResiliencyDemos
{
    public static class Helpers
    {
        public static IHtmlString FormatParagraphs(this HtmlHelper helper, string input)
        {
            return helper.Raw("<p>" + helper.Encode(input).TrimEnd('\n').Replace("\n", "</p>\n<p>") + "</p>");
        }
    }
}