using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCTalk.StreamAnalyzer.Implementation
{
    public class FileExportFilter
    {
        public string ExportDirectory { get; set; }

        public string[] MessageNames { get; set; }

        public bool IncludeResponse { get; set; } = true;

        public string[] GroupByKeys { get; set; }

        public FilterItem[] Conditions { get; set; }

        public string SeparateOnKey { get; set; }
        public string SeparateOnKeyValue { get; set; }


        public string ExportOnlyKey { get; set; }

        public int? ExportWithSpaceSequence { get; set; }
    }
}
