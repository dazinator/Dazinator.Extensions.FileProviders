using System;
using Dazinator.AspNet.Extensions.FileProviders.Directory;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Dazinator.AspNet.Extensions.FileProviders;

namespace FileProvider.Tests
{
    public class DirectoryWatcherTests
    {


        public DirectoryWatcherTests()
        {
            

        }


        [Fact]
        public void Is_Notified_When_Folder_Deleted()
        {
            // Arrange

           // var dirItem = new MockDirectoryItem();

           // var fileInfo = new StringFileInfo("contents", "foo.txt");
            
           // var folderItem = new FolderDirectoryItem("root", null);
           // var childFolder = folderItem.GetOrAddFolder("child");

           // folderItem.Deleted += (sender, e) =>
           // {
           //     DirectoryItemDeletedEventArgs args = e;
           //     args.
           // };

           
           //// childFolder.
           // var sutDirItem = new FileDirectoryItem(
           //     fileInfo);
           // var sut = new DirectoryWatcher(false);
           // sut.Watch(dirItem);

           // dirItem.



        }

        private void FolderItem_Deleted(object sender, DirectoryItemDeletedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }

   
}
