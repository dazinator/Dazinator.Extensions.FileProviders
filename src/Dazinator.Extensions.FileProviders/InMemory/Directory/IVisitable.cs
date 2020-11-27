namespace Dazinator.Extensions.FileProviders.InMemory.Directory
{
    public interface IVisitable<T>
    {
        void Accept(T Visitor);
    }
}
