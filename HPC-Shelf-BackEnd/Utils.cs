using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace HPCBackendServices {
    public class Utils {
        public static string HOME = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/";
        public static string CURRENT_FOLDER = Environment.CurrentDirectory + "/";
        public static string SCRIPTS = Environment.CurrentDirectory + "/scripts/";

        public static string commandExecBash(string command) {
            return Utils.commandExec(new ProcessStartInfo("bash", " " + command));
        }
        public static string commandExecBin(string command) {
            return Utils.commandExec(new ProcessStartInfo(command));
        }
        public static string[] fileNames(string folder) {
            return Directory.GetFiles(folder);
        }

        public static IEnumerable<string> fileContent(string folder, string fileName) {
            return File.ReadLines(folder + "/" + fileName);
        }
        public static void fileWrite(string folder, string fileName, IEnumerable<string> content) {
            File.WriteAllLines(folder + "/" + fileName, content);
        }
        private static string commandExec(ProcessStartInfo processInfo) {
            processInfo.CreateNoWindow = true;
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardError = true;
            processInfo.RedirectStandardOutput = true;
            
            var process = Process.Start(processInfo);
            
            int n = 20;
            string tail = "";
            Console.WriteLine(information("#", n) + " RUN " + information("#", n));
            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => {
                if (!String.IsNullOrEmpty(e.Data))
                    tail = e.Data;
                Console.WriteLine((String.IsNullOrEmpty(e.Data) ? information("-", n) + "-----" + information("-", n) : "#: " + e.Data));
            };
            process.BeginOutputReadLine();
            
            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                Console.WriteLine("error: " + (String.IsNullOrEmpty(e.Data) ? "0" : e.Data));
            process.BeginErrorReadLine();
            
            process.WaitForExit();
            
            Console.WriteLine("ExitCode: {0}", process.ExitCode);
            process.Close();
            return tail;
        }
        private static string information(string c, int n) {
            string info = "";
            for (int i = 0; i < n; i++)
                info += c;
            return info;
        }
    }
}
