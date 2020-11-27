using Dazinator.Extensions.FileProviders.InMemory;
using System;
using System.Reflection;
using Xunit;

namespace Dazinator.Extensions.FileProviders.Tests
{


    public class EmbeddedFileInfoTests
    {

        [Fact]
        public void GetFileInfo_ReturnsFileInfo_ForEmbeddedFile()
        {
            using (var provider = new InMemoryFileProvider())
            {
                var embeddedFileInfo = new EmbeddedFileInfo(GetAssemblyFromType(this.GetType()),
                    $"Dazinator.Extensions.FileProviders.Tests.Resources.myresource.txt",
                    "myfile.txt");

                provider.Directory.AddFile("/some/folder", embeddedFileInfo);

                var info = provider.GetFileInfo("/some/folder/myfile.txt");
                using (var stream = info.CreateReadStream())
                {
                    Assert.NotNull(stream);
                    Assert.True(stream.Length > 0);
                }

            }
        }


        public static Assembly GetAssemblyFromType(Type type)
        {

#if NETSTANDARD
            var assy = type.GetTypeInfo().Assembly;
#else
            var assy = type.Assembly;
#endif
            return assy;
        }



    }
}
