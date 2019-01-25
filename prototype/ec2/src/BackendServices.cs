using System;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Collections.Generic;
using System.Threading;

namespace AWS.Shelf {
    public class BackendServices : IBackendServices {

        protected List<IVirtualMachine> virtual_machines = new List<IVirtualMachine>();
        public List<IVirtualMachine> VirtualMachines { get { return virtual_machines; } }

        private AmazonEC2Client client;
        public BackendServices(){
            client = new AmazonEC2Client(RegionEndpoint.SAEast1);
        }
        public string deploy(string platform_config) {
            string[] config = platform_config.Split('\n', '\t', '\r');

            string keyPairName = "credential";
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/";
            InstancesManager.CreateKeyPair(client, keyPairName, home + keyPairName + ".pem");

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
            }
            catch {
                Console.WriteLine("Invalid Config");
                throw;
            }
            Thread.Sleep(1000);//Wait aws define public ips
            foreach(string instance_id in ids)
                DataRegistry(client, instance_id);
            return instanceId;
        }
        public void undeploy() {
            foreach(IVirtualMachine vm in VirtualMachines){
                InstancesManager.TerminateInstance(vm.InstanceId, client);
            }
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
            List<Reservation> result = client.DescribeInstances(req).Reservations;

            foreach (Amazon.EC2.Model.Reservation reservation in result) {
                foreach (Instance runningInstance in reservation.Instances) {
                    IVirtualMachine vm = new VirtualMachine();
                    vm.InstanceId = runningInstance.InstanceId;
                    vm.PrivateIpAddress = runningInstance.PrivateIpAddress;
                    vm.PublicIpAddress = runningInstance.PublicIpAddress;
                    VirtualMachines.Add(vm);
                }
            }
        }
    }
    public class VirtualMachine: IVirtualMachine{
        private string instanceId;
        public string InstanceId { get { return instanceId; } set { instanceId = value; } }

        private string privateIpAddress;
        public string PrivateIpAddress { get { return privateIpAddress; } set { privateIpAddress = value; } }

        private string publicIpAddress;
        public string PublicIpAddress { get { return publicIpAddress; } set { publicIpAddress = value; } }
    }
}
