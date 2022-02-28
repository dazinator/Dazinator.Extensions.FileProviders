using System.Collections.Generic;
using Microsoft.Extensions.FileProviders;
using System;
using Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets;
using static Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets.StaticWebAssetManifest;

namespace Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets
{
    public static class FileMapExtensions
    {
        public static FileMap AddFromStaticWebAssetsManifest(this FileMap map, StaticWebAssetManifest manifest, Func<string, IFileProvider> contentRootFileProviderFactory)
        {
            PopulateFromManifest(map, manifest, contentRootFileProviderFactory);
            return map;
        }

        private static void PopulateFromManifest(FileMap map,
            StaticWebAssetManifest manifest,
            Func<string, IFileProvider> contentRootFileProviderFactory)
        {
            IFileProvider[] fps = new IFileProvider[manifest.ContentRoots.Length];
            for (int i = 0; i < fps.Length; i++)
            {
                fps[i] = contentRootFileProviderFactory(manifest.ContentRoots[i]);
                // patterns[i] = new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
            }

            Stack<Tuple<string, StaticWebAssetNode, FileMap>> stack = new Stack<Tuple<string, StaticWebAssetNode, FileMap>>();
            stack.Push(new Tuple<string, StaticWebAssetNode, FileMap>(string.Empty, manifest.Root, map));

            while (stack.TryPop(out var tuple))
            {
                var key = tuple.Item1;
                var node = tuple.Item2;
                var requestPathNode = tuple.Item3;

                if (node.Patterns != null)
                {
                    foreach (var pattern in node.Patterns)
                    {
                        var fp = fps[pattern.ContentRoot];
                        requestPathNode.AddPatternMapping(pattern.Pattern, fp);
                    };
                }

                // This should always be null, but could technically be set for root node.
                if (node.Match != null)
                {
                    if (requestPathNode.Parent != null) // are we processing root node
                    {
                        throw new InvalidOperationException("file mappings should be added to parent node");
                    }
                    // if it has an asset, we see this as an individual file mapping that should be added under
                    // the previuos request path node. i.e we don't want new request path nodes added per mapped file.
                    // we wan't a single one for the parent path, which has patterns, and files added to it as seperate concepts individually.
                    var asset = node.Match;
                    var fp = fps[asset.ContentRoot];
                    requestPathNode.AddFileNameMapping($"/{key}", fp, asset.Path);
                }

                if (node.Children == null)
                {
                    continue;
                }

                // stack children t
                foreach (var child in node.Children)
                {
                    // We are only interested in creating additional `RequestPathNode`s to represent each "parent"
                    // segment of the request path,
                    // so, foo/bar/bat.txt and foo/bar/baz.txt--> 
                    //   -foo
                    //     -bar
                    // then the "bat.txt" and "baz.txt" will be added as "file mappings" to the "bar" parent RequestPathNode.
                    // we detect if the child represents a "file mapping" by checking the "Match" property which
                    // points to a mapped file when that is the case.            

                    var asset = child.Value?.Match;
                    if (asset != null)
                    {
                        var fp = fps[asset.ContentRoot];
                        requestPathNode.AddFileNameMapping($"/{child.Key}", fp, asset.Path);
                    }
                    else
                    {
                        // must be processed as a potential parent.                       
                        var nextParentNode = requestPathNode.AddChild($"/{child.Key}");
                        stack.Push(new Tuple<string, StaticWebAssetNode, FileMap>(child.Key, child.Value, nextParentNode));
                    }

                }
            }
        }


        //private StaticWebAssetManifest LoadStaticWebAssetManifest(Stream manifestFileStream)
        //{
        //    var manifest = StaticWebAssetManifest.Parse(manifestFileStream);
        //    return manifest;
        //}

    }



}


