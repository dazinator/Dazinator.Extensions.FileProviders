namespace Dazinator.AspNet.Extensions.FileProviders.Globbing
{
    class Token
    {
        public Token(TokenKind kind, string spelling)
        {
            this.Kind = kind;
            this.Spelling = spelling;
        }

        public TokenKind Kind { get; private set; }
        public string Spelling { get; private set; }
    }
}