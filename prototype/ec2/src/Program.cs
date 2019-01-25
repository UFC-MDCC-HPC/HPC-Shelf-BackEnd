using System;
using System.Threading;

namespace AWS.Shelf { // Ubuntu: sudo apt install nuget
    public class Program {
        public static void Main(string[] args) {
            IBackendServices backend = new BackendServices();
            backend.deploy("ami-0318cb6e2f90d688b\n3");

            Console.WriteLine("****************************************************************************");
            foreach(IVirtualMachine vm in backend.VirtualMachines){
                Console.WriteLine("InstanceId: " + vm.InstanceId + 
                                  "     PrivateIpAddress: " + vm.PrivateIpAddress +
                                  "     PublicIpAddress: " + vm.PublicIpAddress);
            }
            Console.WriteLine("****************************************************************************");

            backend.undeploy();
        }
    }
}
