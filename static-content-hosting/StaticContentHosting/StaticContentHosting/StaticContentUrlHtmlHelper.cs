using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;

namespace StaticContentHosting
{
    public static class StaticContentUrlHtmlHelper
    {
        public static string StaticContentUrl(this IHtmlHelper helper, string contentPath, IConfiguration configuration)
        {
            if (!string.IsNullOrEmpty(configuration["StaticContentBaseUrl"]))
            {
                if (contentPath.StartsWith("~"))
                {
                    contentPath = contentPath.Substring(1);
                }

                contentPath = string.Format("{0}/{1}", configuration["StaticContentBaseUrl"].TrimEnd('/'), contentPath.TrimStart('/'));
            }
            var url = new UrlHelper(helper.ViewContext);

            return url.Content(contentPath);
        }
    }
}
