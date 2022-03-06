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
- MappingFileProvider

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

Allows you to wrap an existing `IFileProvider` but specify a list of include / exclude `glob` patterns to filter the content (files only) that are accessible.
Note: Folder information is not filtered out, only files will be filtered by the glob expressions.
For example, if you have a folder in the underlying provider "/foo" and you wrap it with this `GlobPatternFilterFileProvider` and the include pattern
`bar.txt`, when calling `GetFileInfo("/foo")` it will still return an item for the directory and when calling `GetDirectoryContents("")` it will still include an item for this directory
even though the directory name doesn't strictly match the pattern. This is because the patterns are applied to files only. This was better for optimisation purposes.
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


## MappingFileProvider

Allows you to map files from other sources into a virtual directory of sorts, using a combiation of:-

- Explicit mappings (i.e a mapping that maps a particular file from a source path within a source `IFileProvider`, to the requested path)
- Pattern mappings (i.e a mapping that maps many files or directories onto the request path,from a source `IFileProvider` - where they match the specified `glob` pattern)

### Example of an explicit mapping

```csharp

 var sourceFileProvider = new InMemoryFileProvider();

 // add some source file at `/foo/bar/test.txt`
 sourceFileProvider.Directory.AddFile("/foo/bar", 
                    new StringFileInfo("dummy contents", "test.txt"));


// Create the map first:
var fileMap = new FileMap();
fileMap.MapPath("/my-files", (pathMappings) =>
        {            
          pathMappings.AddFileNameMapping("/test.txt", sourceFileProvider, "/foo/bar/test.txt);        
        });

var mappingFileProvider = new MappingFileProvider(fileMap);
var file = mappingFileProvider.GetFileInfo("/my-files/test.txt");
Assert.True(file.Exists);

```


### Example of a pattern mapping


```csharp
var sourceFileProvider = new InMemoryFileProvider();

 // add some source files at `/foo/bar/test.txt` and `/foo/bar/test.csv`
 sourceFileProvider.Directory.AddFile("/foo/bar", 
                    new StringFileInfo("dummy contents", "test.txt"));
 sourceFileProvider.Directory.AddFile("/foo/bar", 
                    new StringFileInfo("dummy contents", "test.csv"));



// Create the map first:
var fileMap = new FileMap();
fileMap.MapPath("/my-files", (pathMappings) =>
        {            
          pathMappings.AddPatternMapping("**/*.txt", inMemoryFileProvider);               
        });

var mappingFileProvider = new MappingFileProvider(fileMap);
var file = mappingFileProvider.GetFileInfo("/my-files/test.txt");
Assert.True(file.Exists);

var file = mappingFileProvider.GetFileInfo("/my-files/test.csv");
Assert.False(file.Exists);

```


### Pattern Mappings and GetDirectoryContents

When getting the directory contents, the directory is built from all attached source providers in the hierarchy.

For example, if you map `**` from provider A on request path "/foo" and that provider happends to a have a folder in it's root directory named `/bar`
and you also map `**` from provider B on request path "/foo/bar" and that provider has a file named "foo.txt"
When you call `GetDirectoryContents()` for path "/foo/bar"

- The most specific matching mapping is evaluated first, in this case provider B will include "foo.txt" in the returned contents.
- Each parent segment of the request path is then evaluated back to the root "/" so in this case provider A will also include the contents of it's "/bar" folder.


In the case of a conflict (i.e an item is to be included that has the same name) - the first provider evaluated wins, so in this case the provider that maps closest (most explicitly) to the request path wins (or overrides is another way to think about it).
e.g in the case above, provider B overrides any conflicting items from provider A.


### Static Assets

Razor / msbuild tooling in .NET 6 outputs a static web assets manifest file for your projects when you build them locally.
When running an asp.net core host on a Development environment, your application (The Host) discoveres this mapping file and configures an internal `IFileProvider` to asp.net core to create 
a very similar virtual directory structure to what's described here. This is not accessible to you for general use.
The `MappingFileProvider` offers very similar functionality, but in a way that is accessible for use in general applications if desirable.
To facilitate the serving of static assets, there is an extension method that can be used to build a `FileMap` from the native manifest that razor tooling produces.

1. Load the manifest from the file:

```
            var manifestFile = fp.GetFileInfo(resourcePath);
            using (var stream = manifestFile.CreateReadStream())
            {
                var manifest = StaticWebAssetManifest.Parse(stream);              

                return manifest;
            }
```

2. Use the manifest to build a map. Note when building the map from a manifest you must provide a 
factory function to return the `IFileProvider`s that will back the list of `ContentRoot`s specified in the manifest. Typically
you will want to return the PhysicalFileProvider`s. I use InMemory ones as this was test code.

```
 var map = new FileMap();

 map.AddFromStaticWebAssetsManifest(manifest, (contentRoot) =>
 {
     var provider = new InMemoryFileProvider();
     return provider;
 });

 ```

 Now that you have a map you can create a new `MappingFileProvider` as shown previously.
