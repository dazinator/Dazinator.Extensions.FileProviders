using Dazinator.Extensions.FileProviders.InMemory.Directory;
using Xunit;

namespace Dazinator.Extensions.FileProviders.Tests
{
    public class DirectoryWatcherTests
    {

        public DirectoryWatcherTests()
        {


        }


        [Fact]
        public void Can_Watch_Directory_For_New_Items()
        {
            // Arrange
            IDirectory directory = new InMemoryDirectory();
            var watcher = new DirectoryWatcher(directory);

            var watchPattern = "/some/dir/folder/newfile.*";
            watcher.AddFilter(watchPattern);

            //
            bool notified = false;

            watcher.ItemAdded += (sender, e) =>
            {
                DirectoryItemAddedEventArgs args = e.DirectoryItemEventArgs;
                var matchedFilters = e.MatchedFilters;
                IDirectoryItem newItem = args.NewItem;
                Assert.Equal("newfile.txt", newItem.Name);
                notified = true;
            };


            directory.AddFile("/some/dir/folder/", new StringFileInfo("hi", "newfile.txt"));
            Assert.True(notified);

            notified = false;
            directory.AddFile("/some/dir/folder/", new StringFileInfo("shouldnt trigger", "anotherfile.txt"));
            Assert.False(notified);



        }

        [Fact]
        public void Can_Watch_Directory_For_Updated_Items()
        {
            // Arrange
            IDirectory directory = new InMemoryDirectory();
            directory.AddFile("/some/dir/folder/", new StringFileInfo("hi", "newfile.txt"));

            var watcher = new DirectoryWatcher(directory);
            var watchPattern = "/some/dir/folder/new*.txt";
            watcher.AddFilter(watchPattern);

            //
            bool notified = false;

            watcher.ItemUpdated += (sender, e) =>
            {
                DirectoryItemUpdatedEventArgs args = e.DirectoryItemEventArgs;
                var matchedFilters = e.MatchedFilters;
                Assert.Equal("newfile.txt", args.OldItem.Name);
                Assert.Equal("newfile.csv", args.NewItem.Name);
                notified = true;
            };

            // now update the file
            var existingItem = directory.GetFile("/some/dir/folder/newfile.txt");
            existingItem.Update(new StringFileInfo("changed", "newfile.csv"));
            Assert.True(notified);

            notified = false;
            existingItem.Update(new StringFileInfo("changed again", "newfile.csv"));
            // second update shouldnt trigger as our watch pattern is only watching newfile*.txt files.
            Assert.False(notified);



        }

        [Fact]
        public void Can_Watch_Directory_For_Deleted_Items()
        {
            // Arrange
            IDirectory directory = new InMemoryDirectory();
            directory.AddFile("/some/dir/folder/", new StringFileInfo("hi", "foo.txt"));
            directory.AddFile("/some/dir/folder/", new StringFileInfo("hi", "bar.txt"));

            var watcher = new DirectoryWatcher(directory);
            var watchPattern = "/some/dir/folder/foo.txt";
            watcher.AddFilter(watchPattern);

            //
            bool notified = false;

            watcher.ItemDeleted += (sender, e) =>
            {
                DirectoryItemDeletedEventArgs args = e.DirectoryItemEventArgs;
                var matchedFilters = e.MatchedFilters;
                IDirectoryItem deletedItem = args.DeletedItem;
                Assert.Equal("foo.txt", deletedItem.Name);
                notified = true;
            };


            var fooFile = directory.GetFile("/some/dir/folder/foo.txt");
            fooFile.Delete();
            Assert.True(notified);

            notified = false;
            var barFile = directory.GetFile("/some/dir/folder/bar.txt");
            barFile.Delete();
            Assert.False(notified);



        }



        [Fact]
        public void Can_Watch_Directory_For_Moved_Items()
        {
            // Arrange
            // Prepare our in memory directory.
            IDirectory directory = new InMemoryDirectory();
            directory.AddFile("/some/dir/folder/", new StringFileInfo("hi", "newfile.txt"));
            directory.AddFile("/some/dir/another/", new StringFileInfo("hi", "newfile.txt"));
            directory.AddFile("/some/dir/hello/", new StringFileInfo("hi", "newfile.txt"));

            // Watch the directory using a pattern as a filter.
            var watcher = new DirectoryWatcher(directory);
            var watchPattern = "/some/dir/**/new*.txt";
            watcher.AddFilter(watchPattern);

            // Register for the watchers ItemUpdated event, this is how it should notify us of updated
            // files.
            int notifyCount = 0;
            watcher.ItemUpdated += (sender, e) =>
            {
                DirectoryItemUpdatedEventArgs args = e.DirectoryItemEventArgs;
                var matchedFilters = e.MatchedFilters; // contains the patterns that were matched (the reason we are being notified)
                // we aren't renaming the file so the file should have the same name.
                // We are renaming a parent folder, so only the items full path should be different.
                Assert.Equal("newfile.txt", args.OldItem.Name);
                Assert.Equal("newfile.txt", args.NewItem.Name);
                Assert.StartsWith("/some/dir/", args.OldItem.Path); // the old folder path.
                Assert.StartsWith("/newfoldername/dir/", args.NewItem.Path); // the new folder path.

                //Assert.Equal("/changed/dir/folder/newfile.txt", args.NewItem.Path);
                notifyCount = notifyCount + 1;
            };

            // Act
            // Rename the root folder /some too be /newfoldername
            // this should cause the watcher to notifiy us of effected items we are interested in.
            var folder = directory.GetFolder("/some");
            folder.Rename("newfoldername");

            // Watcher should have notified us on of 3 items we were watching (matched by the pattern)
            Assert.Equal(3, notifyCount);

            // now rename the file again - this time the watcher should not notify us, as the items live in a directory
            // that no longer match out pattern - i.e the previous rename operation has moved them outside of our filter pattern.
            notifyCount = 0;
            var existingItem = directory.GetFile("/newfoldername/dir/folder/newfile.txt");
            existingItem.Update(new StringFileInfo("changed", "newfile.csv"));
            Assert.Equal(0, notifyCount);



        }


    }


}
