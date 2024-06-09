namespace Ikiru.Parsnips.Functions.Email
{
    public class EmailContent
    {
        public string Subject { get; }
        public string TextBody { get; }
        public string HtmlBody { get; }

        public EmailContent(string subject, string textBody, string htmlBody)
        {
            Subject = subject;
            TextBody = textBody;
            HtmlBody = htmlBody;
        }
    }
}