using System.IO;

namespace TypeTreeTools
{
    public class Log
    {
        private static StreamWriter stream;
        static Log()
        {
            if(!Directory.Exists("Logs")) Directory.CreateDirectory("Logs");
            stream = new StreamWriter("Logs/TypeTreeToolsLog.txt");
        }
        public static void WriteLine(string text, params object[] args)
        {
            stream.WriteLine(text, args);
            stream.Flush();
        }
        public static void Reset()
        {
            if(stream != null)
            {
                stream.Flush();
                stream.Dispose();
                stream = new StreamWriter("Logs/TypeTreeToolsLog.txt");
                stream.AutoFlush = true;
            }
        }
    }
}
