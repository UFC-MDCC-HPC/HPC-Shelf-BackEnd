using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HPCBackendServices;

namespace Tests {
    public class MainClass {
        public static void Main(string[] args) {

            //Utils.commandExecBin("/bin/hostname");

            //Utils.commandExecBash(Utils.HOME + "teste 4");

            string folder = "/tmp/lista";
            string[] vetor = Utils.fileNames(folder);
            foreach (string s in vetor)
                Console.WriteLine("#: "+s);

            foreach (string line in Utils.fileContent(folder, "a.sh"))
                Console.WriteLine(">>: "+line);

            string[] content = { "1", "2", "3"};
            Utils.fileWrite(folder, "a.sh", content);

            Console.WriteLine("CURRENT_FOLDER: "+Utils.CURRENT_FOLDER);

        }
    }
}
