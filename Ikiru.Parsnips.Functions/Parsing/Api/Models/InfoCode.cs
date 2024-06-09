namespace Ikiru.Parsnips.Functions.Parsing.Api.Models
{
    public enum InfoCode
    {
        // https://docs.sovren.com/API/Rest/Parsing#parse-resume

        Unknown,

        /// <summary>
        /// Successful transaction
        /// </summary>
        Success,

        /// <summary>
        /// A successful transaction that contains warnings in the parsed document result
        /// </summary>
        WarningsFoundDuringParsing,
        
        /// <summary>
        /// The timeout occurred before the document was finished parsing which can result in truncation
        /// </summary>
        PossibleTruncationFromTimeout,

        /// <summary>
        /// There was an issue converting the document
        /// </summary>
        ConversionException,

        /// <summary>
        /// A required parameter wasn't provided
        /// </summary>
        MissingParameter,

        /// <summary>
        /// A parameter was incorrectly specified
        /// </summary>

        InvalidParameter,

        /// <summary>
        /// An error occurred with the credentials provided
        /// </summary>
        AuthenticationError
    }
}