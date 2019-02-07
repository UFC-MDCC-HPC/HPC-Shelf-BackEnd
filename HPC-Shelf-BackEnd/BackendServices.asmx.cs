using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Threading;

namespace HPCBackendServices {
    public class BackendServices : System.Web.Services.WebService, IBackendServices {

        protected static List<IVirtualMachine> virtual_machines = new List<IVirtualMachine>();
        public static List<IVirtualMachine> VirtualMachines { get { return virtual_machines; } }

        private AmazonEC2Client client;

        private static RegionEndpoint regiao = RegionEndpoint.USEast1; //RegionEndpoint.USEast1=Virginia // RegionEndpoint.SAEast1=Sao Paulo

        private static string ScriptRunException = "Script RunException!!!";

        public BackendServices() { client = new AmazonEC2Client(regiao); }

        [WebMethod]
        public string deploy(string platform_config) {
            VirtualMachines.Clear();
            string[] config = platform_config.Split('\n', '\t', '\r', ';',' ', ',');

            string keyPairName = "credential";
            InstancesManager.CreateKeyPair(client, keyPairName, Utils.HOME + keyPairName + ".pem");

            string securityGroupName = "hpc-shelf";

            string instanceId = "";
            List<string> ids = new List<string>();
            try {
                string amID = config[0];
                int n = int.Parse(config[1]);

                for (int i = 0; i < n; i++) {
                    instanceId = InstancesManager.LaunchInstance(amID, keyPairName, securityGroupName, client);
                    ids.Add(instanceId);
                }
            } catch {
                Console.WriteLine("Invalid Config");
                throw;
            }
            Thread.Sleep(1000);
            int current = 0;
            while (current < ids.Count) {
                Console.Write("Status ");
                string instance_id = ids.ElementAt(current);
                string status = InstancesManager.Instance_status(regiao, instance_id);
                Console.WriteLine(instance_id + ": " + status);
                if (!status.Equals("running")){
                    Console.WriteLine("Waiting to running: " + instance_id);
                    Thread.Sleep(5000);
                } else {
                    current++;
                    DataRegistry(client, instance_id);
                }
            }
            generateSlavesHosts();

            if (VirtualMachines.Count > 0)
                return VirtualMachines.ElementAt(0).PublicIpAddress;
            return "";
        }
        [WebMethod]
        public string status(){
            int current = 0;
            int size = 0;
            while (current < VirtualMachines.Count) {
                Console.Write("Status ");
                string instance_id = VirtualMachines.ElementAt(current).InstanceId;
                string status = InstancesManager.Instance_statusCheck(regiao, instance_id);
                Console.WriteLine(instance_id + ": " + status);
                if (!status.Equals("ok")) {
                    Console.WriteLine("Waiting to ok: " + instance_id);
                    Thread.Sleep(100);
                } else size++;
                current++;
            }
            return (size == VirtualMachines.Count)? "OK" : "NO";
        }
        [WebMethod]
        public string mpi(){
            try {
                if (VirtualMachines.Count > 0) {
                    Utils.commandExecBash(Utils.SCRIPTS + "run1");
                    Utils.commandExecBash(Utils.SCRIPTS + "run2");
                    return VirtualMachines.ElementAt(0).PublicIpAddress;
                }
            } catch (Exception e) {
                Console.WriteLine(ScriptRunException);
            }
            return ScriptRunException;
        }
        [WebMethod]
        public string startShelf() {
            try {
                if (VirtualMachines.Count > 0) {
                    string master = VirtualMachines.ElementAt(0).PublicIpAddress;
                    Utils.commandExecBash(Utils.SCRIPTS + "startShelf "+master+" &");
                    return master;
                }
            } catch (Exception e) {
                Console.WriteLine(ScriptRunException);
            }
            return ScriptRunException;
        }

        protected void DataRegistry(AmazonEC2Client client, string instanceId) {
            DescribeInstancesRequest req = new DescribeInstancesRequest() {
                Filters = new List<Filter>() {
                    new Filter() {
                        Name = "instance-id",
                        Values = new List<String>() { instanceId }
                    }
                }
            };
            IVirtualMachine vm = null;
            while (vm == null || vm.PublicIpAddress == null) {
                List<Reservation> result = client.DescribeInstances(req).Reservations;

                foreach (Amazon.EC2.Model.Reservation reservation in result) {
                    foreach (Instance runningInstance in reservation.Instances) {
                        vm = new VirtualMachine {
                            InstanceId = runningInstance.InstanceId,
                            PrivateIpAddress = runningInstance.PrivateIpAddress,
                            PublicIpAddress = runningInstance.PublicIpAddress
                        };
                    }
                }
                if (vm == null || vm.PublicIpAddress == null) Thread.Sleep(1000); //Wait 1 min for aws release public IP address
            }
            VirtualMachines.Add(vm);
        }
        protected void generateSlavesHosts() {
            if (VirtualMachines.Count > 0) {
                IEnumerator<IVirtualMachine> it = VirtualMachines.GetEnumerator();
                it.MoveNext();

                List<string> hosts = new List<string>();
                List<string> slaves = new List<string>();
                List<string> address = new List<string>();
                List<string> workers = new List<string>();
                int count = 0; int ports = 4801;

                slaves.Add("master");
                hosts.Add("127.0.0.1 localhost");
                hosts.Add(it.Current.PrivateIpAddress + " master");
                address.Add(it.Current.PublicIpAddress);
                workers.Add("-n 1 /opt/mono-4.2.2/bin/mono-service -l:Worker"+(ports)+".lock bin/WorkerService.exe --port "+(ports)+" --debug --no-deamon");
                ports++;

                while (it.MoveNext()) {
                    IVirtualMachine vm = it.Current;

                    slaves.Add("slave" + (++count));
                    hosts.Add(vm.PrivateIpAddress + " slave"+count);
                    address.Add(vm.PublicIpAddress);

                    workers.Add("-n 1 /opt/mono-4.2.2/bin/mono-service -l:Worker" + (ports) + ".lock bin/WorkerService.exe --port " + (ports) + " --debug --no-deamon");
                    ports++;
                }
                Utils.fileWrite(Utils.SCRIPTS, "slaves", slaves.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "hosts", hosts.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "address", address.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "workers", workers.ToArray());
            }
        }
    }
}
