//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace BSAG.IOCTalk.Logging.Extentions
//{
//    public class ExtentionsLogging : BSAG.IOCTalk.Common.Interface.Logging.ILogger //: ILoggerProvider, ILogger
//    {
//        private ILogger logger;

//        public ExtentionsLogging()
//        {
//            ILoggerFactory loggerFactory = new LoggerFactory()
//                .AddConsole(LogLevel.Debug);
            

//            loggerFactory.AddProvider(new BasicFileLoggerExtention());
//            this.logger = loggerFactory.CreateLogger("main");
//        }

//        public ExtentionsLogging(ILoggerFactory loggerFactory, string iocTalkLoggerCategory)
//        {
//            this.logger = loggerFactory.CreateLogger(iocTalkLoggerCategory);
//        }

//        public void Debug(string message)
//        {
//            logger.LogDebug(message);
//        }

//        public void Error(string message)
//        {
//            logger.LogError(message);
//        }

//        public void Info(string message)
//        {
//            logger.LogInformation(message);
//        }

//        public void Warn(string message)
//        {
//            logger.LogWarning(message);
//        }

       
//    }
//}
