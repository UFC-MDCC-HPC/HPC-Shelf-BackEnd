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

        protected List<IVirtualMachine> virtual_machines = new List<IVirtualMachine>();
        public List<IVirtualMachine> VirtualMachines { get { return virtual_machines; } }

        private AmazonEC2Client client;

        //public BackendServices() { client = new AmazonEC2Client(RegionEndpoint.SAEast1); } //Sao Paulo
        public BackendServices() { client = new AmazonEC2Client(RegionEndpoint.USEast1); } //Virginia

        [WebMethod]
        public string deploy(string platform_config) {
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
            Thread.Sleep(1000);//Wait aws define public ips
            foreach (string instance_id in ids)
                DataRegistry(client, instance_id);
            if (VirtualMachines.Count > 0)
                return VirtualMachines.ElementAt(0).PublicIpAddress;
            return "";
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
    }
    public class VirtualMachine : IVirtualMachine {
        private string instanceId;
        public string InstanceId { get { return instanceId; } set { instanceId = value; } }

        private string privateIpAddress;
        public string PrivateIpAddress { get { return privateIpAddress; } set { privateIpAddress = value; } }

        private string publicIpAddress;
        public string PublicIpAddress { get { return publicIpAddress; } set { publicIpAddress = value; } }
    }
}
