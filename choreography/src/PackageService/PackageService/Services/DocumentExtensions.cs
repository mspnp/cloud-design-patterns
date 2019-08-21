using Microsoft.Azure.Documents;
using PackageService.Models;

namespace PackageService.Services
{
    public static class DocumentExtensions
    {
        public static Package ToPackage(this Document document)
        {
            Package package = new Package(
                    document.GetPropertyValue<string>("id"),
                    document.GetPropertyValue<PackageSize>("size"),
                    document.GetPropertyValue<double>("weight"),
                    document.GetPropertyValue<string>("tag"));

            return package;
        }

        public static Document ToDocument (this Package package)
        {
            Document document = new Document();

            document.SetPropertyValue("id", package.Id);
            document.SetPropertyValue("size", package.Size);
            document.SetPropertyValue("weight", package.Weight);
            document.SetPropertyValue("tag", package.Tag);

            return document;
        }
    }
}
