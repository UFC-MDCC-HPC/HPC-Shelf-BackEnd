using System;
using System.Diagnostics;
using HPCBackendServices;

namespace Tests {
    public class MainClass {
        public static void Main(string[] args) {

            Utils.commandExecBin("/bin/hostname");

            Utils.commandExecBash(Utils.HOME + "teste 4");
        
        }
    }
}
