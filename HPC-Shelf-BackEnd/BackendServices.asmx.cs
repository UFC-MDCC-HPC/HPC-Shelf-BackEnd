using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Services;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace br.ufc.mdcc.hpcshelf.backend
{
    public interface IBackendServices
	{
		string deploy(string platform_config);
	}


	public class BackendServices : WebService, IBackendServices {

        protected static List<IVirtualMachine> virtual_machines = new List<IVirtualMachine>();
        public static List<IVirtualMachine> VirtualMachines { get { return virtual_machines; } }

        private AmazonEC2Client client;

        private static RegionEndpoint regiao = RegionEndpoint.USEast1; //RegionEndpoint.USEast1=Virginia // RegionEndpoint.SAEast1=Sao Paulo

        private static string ScriptRunException = "Script RunException!!!";

        private static string default_instance_type = InstanceType.T2Micro.ToString();

        public BackendServices() {  }

        [WebMethod]
        public string deploy(string platform_config) 
        {
            Console.WriteLine("STARTING DEPLOYING -- {0}", platform_config == null);
            Console.WriteLine("PLATFORM: {0}", platform_config);

			string[] config = platform_config.Split('\n', '\t', '\r', ';', ' ', ',');
			
            regiao = config.Length <= 3 || config[3] == "" ? regiao : RegionEndpoint.GetBySystemName(config[3]); 
            client = new AmazonEC2Client(regiao);

			VirtualMachines.Clear();

            string keyPairName = "credential";
            InstancesManager.CreateKeyPair(client, keyPairName, Utils.HOME + keyPairName + ".pem");

            string securityGroupName = "hpc-shelf";

            string instanceId = "";
            List<string> ids = new List<string>();
            try 
            {
                string amID = config[0];
                int n = int.Parse(config[1]);
                string instance_type = config.Length <= 2 || config[2] == "" ? default_instance_type : config[2];
                Console.WriteLine("INSTACE TYPE is {0}", instance_type);

                for (int i = 0; i < n; i++) 
                {
                    instanceId = InstancesManager.LaunchInstance(instance_type, amID, keyPairName, securityGroupName, client);
                    ids.Add(instanceId);
                }
            } 
            catch 
            {
                Console.WriteLine("Invalid Config");
                throw;
            }

            Thread.Sleep(1000);
            int current = 0;
  
            while (current < ids.Count) 
            {
                Console.Write("Status ");
                string instance_id = ids.ElementAt(current);
                //string status = InstancesManager.Instance_status(regiao, instance_id);
                string status_check = InstancesManager.Instance_statusCheck(regiao, instance_id);
                Console.WriteLine(instance_id + ": " + status_check);
                if (!status_check.Equals("ok")){
                    Console.WriteLine("Waiting to running: " + instance_id);
                    Thread.Sleep(5000);
                } else {
                   current++;
                    DataRegistry(client, instance_id);
                }
            }
			string platform_address = VirtualMachines.ElementAt(0).PublicIpAddress;
			string platform_address_local = VirtualMachines.ElementAt(0).PrivateIpAddress;

			generatePeerHosts();
            mpi(platform_address, platform_address_local);
            startShelf(platform_address, platform_address_local);

            if (VirtualMachines.Count > 0)
            {
                int port_platform = 8080;

                return "http://" + platform_address + ":" + port_platform + "/PlatformServices.asmx"; ;
            }
            
            return "";
        }


        public string status()
        {
            int current = 0;
            int size = 0;
            while (current < VirtualMachines.Count) 
            {
                Console.Write("Status ");
                string instance_id = VirtualMachines.ElementAt(current).InstanceId;
                string status = InstancesManager.Instance_statusCheck(regiao, instance_id);
                Console.WriteLine(instance_id + ": " + status);
                if (!status.Equals("ok")) 
                {
                    Console.WriteLine("Waiting to ok: " + instance_id);
                    Thread.Sleep(100);
                } else size++;
                current++;
            }
            return (size == VirtualMachines.Count)? "OK" : "NO";
        }

        public string mpi(string platform_address_global, string platform_address_local)
        {
            if (VirtualMachines.Count > 0) 
            {
                try 
                {
                    Utils.commandExecBash(Utils.SCRIPTS + "run1");
                    Utils.commandExecBash(Utils.SCRIPTS + "run2");
                } 
                catch (Exception e) 
                {
                    Console.WriteLine(ScriptRunException);
                }
                return platform_address_global;
            }
            return "VirtualMachines List is empty!!";
        }

        public string startShelf(string platform_address_global, string platform_address_local) 
        {
            if (VirtualMachines.Count > 0) 
            {
                try 
                {
                    Thread thread = new Thread(() => xsp4_shelf_root_run(platform_address_global, platform_address_local));
                    thread.Start();
                    //xsp4_shelf_root_run(platform_address_global, platform_address_local);
                } 
                catch (Exception e) 
                {
                    Console.WriteLine(ScriptRunException);
                }
                return platform_address_global;
            }
            return "VirtualMachines List is empty!!";
        }

        private static void xsp4_shelf_root_run(string platform_address_global, string platform_address_local)
        {
            Utils.commandExecBash(Utils.SCRIPTS + "startShelf " + platform_address_global + " " +  platform_address_local + " &");
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

        protected void generatePeerHosts() 
        {
            if (VirtualMachines.Count > 0)
            {
                IEnumerator<IVirtualMachine> it = VirtualMachines.GetEnumerator();
                it.MoveNext();

                List<string> hosts = new List<string>();
                List<string> peers = new List<string>();
                List<string> address = new List<string>();
                List<string> workers = new List<string>();
				List<string> workers_host_file = new List<string>();
				int count = 0;
                // int portWorkers = 4000;

                peers.Add("root");
                hosts.Add("127.0.0.1 localhost");
                hosts.Add(it.Current.PrivateIpAddress + " root");
                address.Add(it.Current.PublicIpAddress);
                // workers.Add("-n 1 /opt/mono-4.2.2/bin/mono-service bin/WorkerService.exe --port "+(portWorkers)+" --debug --no-deamon");
                workers.Add("-n 1 /opt/mono-4.2.2/bin/mono-service -l:Worker.lock /home/ubuntu/backendservices/bin/WorkerService.exe --port 5000 --debug --no-deamon");
                workers_host_file.Add(it.Current.PrivateIpAddress + " 5000");

                while (it.MoveNext())
                {
                    IVirtualMachine vm = it.Current;

                    peers.Add("peer" + (count));
                    hosts.Add(vm.PrivateIpAddress + " peer" + count);
                    address.Add(vm.PublicIpAddress);
                    //  workers.Add("-n 1 /opt/mono-4.2.2/bin/mono-service bin/WorkerService.exe --port " + (portWorkers) + " --debug --no-deamon");
                    workers.Add("-n 1 /opt/mono-4.2.2/bin/mono-service -l:Worker.lock /home/ubuntu/backendservices/bin/WorkerService.exe --port 5000 --debug --no-deamon");
					workers_host_file.Add(it.Current.PrivateIpAddress + " 5000");
					count++;
                }
                Utils.fileWrite(Utils.SCRIPTS, "peers", peers.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "hosts", hosts.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "address", address.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "workers", workers.ToArray());
                Utils.fileWrite(Utils.SCRIPTS, "workers_host_file", workers_host_file.ToArray());
            }
        }
    }
}
