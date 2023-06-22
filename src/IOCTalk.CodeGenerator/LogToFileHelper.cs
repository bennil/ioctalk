using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace IOCTalk.CodeGenerator
{
#if DEBUG
    internal static class LogToFileHelper
    {
        public static StringBuilder Logs { get; } = new();

        public static void WriteLog(string msg)
        {
            Debug.WriteLine(msg);   // debugging session console output

            if (Logs.Length > 1000_000)
            {
                Logs.Clear();
                WriteLog("[Log cleared] - buffer full");
            }

            Logs.Append("//\t");
            Logs.AppendLine(msg);
        }


        //public static void FlushLogs(SourceProductionContext context)
        //{
        //    WriteLog($"GenTime utc: {DateTime.UtcNow}");

        //    context.AddSource($"logs.g.cs", SourceText.From(Logs.ToString(), Encoding.UTF8));
        //}

        public static string GetLogsCodeText()
        {
            WriteLog($"GenTime utc: {DateTime.UtcNow}");

            return Logs.ToString();
        }
    }
#endif
}
