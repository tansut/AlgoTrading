using System;

namespace Kalitte.Trading.Tests
{
    public class TestAlgo: ILogProvider
    {
        public void Log(string text, LogLevel level = LogLevel.Info, DateTime? t = null)
        {
            //if ((int)level >= this.LoggingLevel)
            {
                string opTime = t.HasValue ? t.Value.ToString("yyyy.MM.dd HH:mm:sss") + "*" : DateTime.Now.ToString("yyyy.MM.dd HH:mm:sss");
                var content = $"[{level}:{opTime}]: {text}" + Environment.NewLine;

                Console.Write(content);
                //var file = LogFile;

                ////var bytes = Encoding.UTF8.GetBytes(content);
                ////logStream.Write(bytes, 0, bytes.Length);
                //File.AppendAllText(file, content + Environment.NewLine);
            }
        }
    }
}