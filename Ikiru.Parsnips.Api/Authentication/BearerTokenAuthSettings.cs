namespace Ikiru.Parsnips.Api.Authentication
{
    public class BearerTokenAuthSettings
    {
        private string m_Authority;

        public string Authority
        {
            get => m_Authority;
            set => m_Authority = value.TrimEnd('/');
        }

        public string Audience { get; set; }
        public int AllowedClockSkewMs { get; set; }
    }
}
