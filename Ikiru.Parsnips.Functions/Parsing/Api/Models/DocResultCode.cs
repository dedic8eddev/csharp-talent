namespace Ikiru.Parsnips.Functions.Parsing.Api.Models
{
    public enum DocResultCode
    {
        // https://docs.sovren.com/Documentation/ResumeParser#document-conversion-result-codes

        // ReSharper disable InconsistentNaming

        Unknown,

        // Success
        ovIsProbablyValid,

        // Warning
        ovProbableGarbageInText,
        ovUnknown,
        ovAvgWordLengthGreaterThan20,
        ovAvgWordLengthLessThan4,
        ovTooFewLineBreaks,
        ovLinesSeemTooShort,
        ovTruncated,

        // Error
        ovConfigurationError,
        ovCorrupt,
        ovCouldNotLoadFile,
        ovErrorOnOutputToHtml,
        ovErrorOnOutputToRtf,
        ovErrorOnOutputToText,
        ovErrorOnOutputToXml,
        ovErrorOnOutputToPdf,
        ovFileNotFound,
        ovIsEncrypted,
        ovIsImage,
        ovNullInput,
        ovTimeout,
        ovUnsupportedFormat,
        ovWordConvErrorAndProbableProblems,
        ovNoText

        // ReSharper restore InconsistentNaming
    }
}