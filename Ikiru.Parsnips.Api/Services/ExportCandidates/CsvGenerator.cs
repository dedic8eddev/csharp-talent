using CsvHelper;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ikiru.Parsnips.Api.Services.ExportCandidates
{
    public class CsvGenerator
    {
        public async Task<byte[]> Run<T>(IEnumerable<T> items, CancellationToken cancellationToken)
        {
            await using var stream = new MemoryStream();
            await using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            await using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

            await csvWriter.WriteRecordsAsync(items);
            
            await streamWriter.FlushAsync();

            stream.Position = 0;

            return stream.ToArray();
        }
    }
}
