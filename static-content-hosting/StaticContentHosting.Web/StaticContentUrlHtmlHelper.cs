// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace StaticContentHosting.Web
{
    using System.Web.Mvc;

    public static class StaticContentUrlHtmlHelper
    {
        public static string StaticContentUrl(this HtmlHelper helper, string contentPath)
        {
            if (contentPath.StartsWith("~"))					
            {
                contentPath = contentPath.Substring(1);
            }

            contentPath = string.Format("{0}/{1}", Settings.StaticContentBaseUrl.TrimEnd('/'), contentPath.TrimStart('/'));

            var url = new UrlHelper(helper.ViewContext.RequestContext);

            return url.Content(contentPath);
        }
    }
}