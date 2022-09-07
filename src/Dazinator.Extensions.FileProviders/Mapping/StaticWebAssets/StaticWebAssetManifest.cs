using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dazinator.Extensions.FileProviders.Mapping.StaticWebAssets
{
#nullable enable

    public sealed class StaticWebAssetManifest
    {
        internal static readonly StringComparer PathComparer =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        public string[] ContentRoots { get; set; } = Array.Empty<string>();

        public StaticWebAssetNode Root { get; set; } = null!;

        public static StaticWebAssetManifest Parse(Stream manifest)
        {
            return JsonSerializer.Deserialize<StaticWebAssetManifest>(manifest)!;
        }
        public sealed class StaticWebAssetNode
        {
            [JsonPropertyName("Asset")]
            public StaticWebAssetMatch? Match { get; set; }

            [JsonConverter(typeof(OSBasedCaseConverter))]
            public Dictionary<string, StaticWebAssetNode>? Children { get; set; }

            public StaticWebAssetPattern[]? Patterns { get; set; }

            // [MemberNotNullWhen(true, nameof(Children))]
            internal bool HasChildren() => Children != null && Children.Count > 0;

            // [MemberNotNullWhen(true, nameof(Patterns))]
            internal bool HasPatterns() => Patterns != null && Patterns.Length > 0;
        }

        public sealed class StaticWebAssetMatch
        {
            [JsonPropertyName("ContentRootIndex")]
            public int ContentRoot { get; set; }

            [JsonPropertyName("SubPath")]
            public string Path { get; set; } = null!;
        }

        public sealed class StaticWebAssetPattern
        {
            [JsonPropertyName("ContentRootIndex")]
            public int ContentRoot { get; set; }

            public int Depth { get; set; }

            public string Pattern { get; set; } = null!;
        }

        public sealed class OSBasedCaseConverter : JsonConverter<Dictionary<string, StaticWebAssetNode>>
        {
            public override Dictionary<string, StaticWebAssetNode> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var parsed = JsonSerializer.Deserialize<IDictionary<string, StaticWebAssetNode>>(ref reader, options)!;
                var result = new Dictionary<string, StaticWebAssetNode>(PathComparer);
                MergeChildren(parsed, result);
                return result;

                static void MergeChildren(
                    IDictionary<string, StaticWebAssetNode> newChildren,
                    IDictionary<string, StaticWebAssetNode> existing)
                {
                    foreach (var item in newChildren)
                    {
                        if (!existing.TryGetValue(item.Key, out var existingNode))
                        {
                            existing.Add(item.Key, item.Value);
                        }
                        else
                        {
                            if (item.Value.Patterns != null)
                            {
                                if (existingNode.Patterns == null)
                                {
                                    existingNode.Patterns = item.Value.Patterns;
                                }
                                else
                                {
                                    if (item.Value.Patterns.Length > 0)
                                    {
                                        var newList = new StaticWebAssetPattern[existingNode.Patterns.Length + item.Value.Patterns.Length];
                                        existingNode.Patterns.CopyTo(newList, 0);
                                        item.Value.Patterns.CopyTo(newList, existingNode.Patterns.Length);
                                        existingNode.Patterns = newList;
                                    }
                                }
                            }

                            if (item.Value.Children != null)
                            {
                                if (existingNode.Children == null)
                                {
                                    existingNode.Children = item.Value.Children;
                                }
                                else
                                {
                                    if (item.Value.Children.Count > 0)
                                    {
                                        MergeChildren(item.Value.Children, existingNode.Children);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, StaticWebAssetNode> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }
        }
    }

#nullable disable
}
