# Dazinator.AspNet.Extensions.FileProviders

[![Build status](https://ci.appveyor.com/api/projects/status/v6w8sn7feb01iypn/branch/master?svg=true)](https://ci.appveyor.com/project/dazinator/dazinator-aspnet-extensions-fileproviders/branch/master)

[![Build status](https://ci.appveyor.com/api/projects/status/v6w8sn7feb01iypn?svg=true)](https://ci.appveyor.com/project/dazinator/dazinator-aspnet-extensions-fileproviders)


Provides some additonal `IFileProvider` implementations that others may find useful.


## RequestPathFileProvider 

This file provider can wrap an existing `IFileProvider` but allows you to prepend information to the path that it's files are resolved on.

For example, let's say you have an `IFileProvider` which resolves a file on the subpath `/myfile.txt`, however, when you serve the file using MVC in the browser, you want it's path to be `/specialfiles/myfile.txt`. 

You can do this:

```csharp
          
            var originalFileProvider = new PhysicalFileProvider(someDir);
            var sut = new RequestPathFileProvider("/specialfiles", originalFileProvider);

```

Now you can resolve exactly the same files and directories through the `RequestPathFileProvider` that the original `FileProvider` has, but you must do so using subpaths that have `/specialfiles` prepended.
