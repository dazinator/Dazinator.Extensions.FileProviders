| Branch  | Build Status | NuGet |
| ------------- | ------------- | ----- |
| Master  |[![Build master](https://ci.appveyor.com/api/projects/status/v6w8sn7feb01iypn/branch/master?svg=true)](https://ci.appveyor.com/project/dazinator/dazinator-extensions-fileproviders/branch/master) | [![NuGet](https://img.shields.io/nuget/v/Dazinator.Extensions.FileProviders.svg)](https://www.nuget.org/packages/Dazinator.Extensions.FileProviders/) |
| Develop | [![Build develop](https://ci.appveyor.com/api/projects/status/v6w8sn7feb01iypn?svg=true)](https://ci.appveyor.com/project/dazinator/dazinator-extensions-fileproviders/branch/develop)  | [![NuGet](https://img.shields.io/nuget/vpre/Dazinator.Extensions.FileProviders.svg)](https://www.nuget.org/packages/Dazinator.Extensions.FileProviders/) |

# Dazinator.Extensions.FileProviders

Provides some useful `IFileProvider` implementations for `asp.net core`.
Current FileProvider Implementations include:

- PrependBasePathFileProvider
- InMemoryFileProvider
- GlobPatternFilterFileProvider

## PrependBasePathFileProvider 

This file provider can wrap an existing `IFileProvider` but allows you to prepend information to the path that it's content is resolved on.

For example, let's say you have an `IFileProvider` which returns a file on the subpath `/myfile.txt`, however, when you serve the file using MVC in the browser, you want it's path to be `/specialfiles/myfile.txt`. 

You can do this:

```csharp
          
            var originalFileProvider = new PhysicalFileProvider(someDir);
            var sut = new PrependBasePathFileProvider("/specialfiles", originalFileProvider);

```

Now you can resolve exactly the same content (files and directories), but now it's via a path with `/specialfiles` prepended.

## InMemoryFileProvider 

Allows you to provide files from an in memory directory.
For example:

```

             // Arrange
            var provider = new InMemoryFileProvider();
            provider.Directory.AddFile("/some/path/", new StringFileInfo("file contents", "foo.txt"));
            // Act
            var fileInfo = provider.GetFileInfo("/some/path/foo.txt");

            // Assert
            Assert.NotNull(fileInfo);
            Assert.True(fileInfo.Exists);

```

The file provider wraps an `IDirectory` which supports the kind of operations you would expect of a directory.
You can set up the directory first, or perform operations on the `IDirectory` independent of the file provider.

For example:

```

            // Arrange
            IDirectory directory = new InMemoryDirectory();

            // Act
            // Adds the specified folder structure to this directory:
            var folder = directory.GetOrAddFolder("/some/dir/folder");
            
            // could add / update, delete files in this directory etc.
            Assert.NotNull(folder);

            var provider = new InMemoryFileProvider(directory);           

```

You can add, update, delete files in the `IDirectory` as you might expect. You can use the `StringFileInfo` class which represents in memory file.

The following adds a file to the directory at `/some/dir/foo.txt` with the contents "contents":


```
var file = directory.AddFile("/some/dir", new StringFileInfo("contents","foo.txt"));

```

The `InMemoryFileProvider` fully supports `watching` and change tokens. Which means if you add / update / delete a file or folder in the directory, the appropriate change tokens will be signalled.

## GlobPatternFilterFileProvider

Allows you to wrap an existing `IFileProvider` but specify a list of include / exclude `glob` patterns to filter the content (files and directories) that is accessible.

Example:

```

            var dir = System.IO.Directory.GetCurrentDirectory();
            var physicalFileProvider = new PhysicalFileProvider(dir); // underlying file provider.

            var includeGlob = "/TestDir/AnotherFolder/**";
            var sut = new GlobPatternFilterFileProvider(physicalFileProvider, new string[] { includeGlob });

            // Act
            var file = sut.GetFileInfo("/TestDir/TestFile.txt"); // this file exists in the underlying provider but is being filtered out.
            Assert.False(file.Exists);

            file = sut.GetFileInfo("/TestDir/AnotherFolder/AnotherTestFile.txt");
            Assert.True(file.Exists);         
```

Note: you can pass in an array of both `include` and `exclude` glob expressions. Glob evaluation is done via the `DotNet.Glob` dependency.