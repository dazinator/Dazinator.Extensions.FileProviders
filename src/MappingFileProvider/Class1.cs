namespace MappingFileProvider
{
    /// <summary>
    /// Maps requests for files to an underlying IFileProvider, amending the original request path
    /// based on path mapping information supplied.
    /// </summary>
    public class MappingFileProvider : IFileProvider
    {
        public MappingFileProvider()
        {
        }
    }
}
