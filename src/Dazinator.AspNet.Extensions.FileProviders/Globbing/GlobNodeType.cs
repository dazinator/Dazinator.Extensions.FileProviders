namespace Dazinator.AspNet.Extensions.FileProviders.Globbing
{
    enum GlobNodeType
    {
        CharacterSet, //string, no children
        Tree, // children
        Identifier, //string
        LiteralSet, //children
        PathSegment, //children
        Root, //string 
        WildcardString, //string
        CharacterWildcard, //string
        DirectoryWildcard,
    }
}