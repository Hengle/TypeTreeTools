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
            stream.AutoFlush = true;
        }
        public static void WriteLine(string text, params object[] args)
        {
            stream.WriteLine(text, args);
        }
    }
}
