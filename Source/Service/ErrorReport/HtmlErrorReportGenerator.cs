using System;
using System.IO;
using System.Text;

namespace Service.ErrorReport
{
    public class HtmlErrorReportGenerator : IErrorReportGenerator
    {
        private const string DefaultMessage = "The file does not comply with the current policy";

        private readonly IFileProcessorConfig _config;

        public HtmlErrorReportGenerator(IFileProcessorConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public string CreateReport(string fileId)
        {
            using (var reader = File.OpenText("Templates/RebuildErrorReportTemplate.html"))
            {
                var report = reader.ReadToEnd();

                var builder = new StringBuilder(report);

                builder.Replace("{{MESSAGE}}", _config.RebuildReportMessage ?? DefaultMessage);
                builder.Replace("{{FILE_ID}}", fileId);

                return builder.ToString();
            }
        }
    }
}
