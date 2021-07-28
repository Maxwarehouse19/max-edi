using System;
using System.Configuration;
using System.IO;

namespace max_edi
{
    public class Logguer
    {
        public static void LogDuration(DateTime started, string message)
        {
            double lasted_sec = Math.Round((DateTime.Now - started).TotalSeconds, 0);
            double lasted_min = Math.Round(lasted_sec/60, 0);
            message = string.Format("{2} started at {0} and lasted {1} seconds ({3})",
                started.ToString("HH:mm:ss"), lasted_sec, message, lasted_min);
            Log(message);
        }
        public static void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
            LogToFile(message);
        }
        public static void LogToFile(string message)
        {
            string basePath = ConfigurationManager.AppSettings["BasePath"];
            string logRelativePath = ConfigurationManager.AppSettings["logRelativePath"];
            string logBaseName = ConfigurationManager.AppSettings["LogBaseName"];

            string logPath = Path.Combine(basePath, logRelativePath);
            Directory.CreateDirectory(logPath);
            string logFile = logBaseName + "_" + DateTime.Now.ToString("yyyyMMdd") + ".txt";
            logFile = Path.Combine(logPath, logFile); 


            StreamWriter logWriter = new StreamWriter(logFile, true);
            logWriter.WriteLine(message);
            logWriter.Close();

        }
    }
}
