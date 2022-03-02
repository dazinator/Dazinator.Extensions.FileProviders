using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public static class TestExtensions
    {
        public static StaticWebAssetManifest LoadStaticWebAssetManifestFromEmbeddedResource(string resourcePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fp = new ManifestEmbeddedFileProvider(assembly);
            var manifestFile = fp.GetFileInfo(resourcePath);
            using (var stream = manifestFile.CreateReadStream())
            {
                var manifest = StaticWebAssetManifest.Parse(stream);
                return manifest;
            }
        }
    }

}

