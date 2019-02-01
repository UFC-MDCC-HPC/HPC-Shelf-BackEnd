using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using HPCBackendServices;
using System.Linq;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Threading;
using System.Web;
using System.Web.Services;


namespace Tests {
    public class MainClass {

        public static void Main(string[] args) {
            //Console.WriteLine("ULTIMO: " + Utils.commandExecBin("/bin/hostname"));

            //string folder = "/home/cenez/workspace/gits/HPC-Shelf-BackEnd/HPC-Shelf-BackEnd/scripts/";
            //string ultimo = "X"; 
            //while(!ultimo.Substring(0,3).Equals("ip-"))
            //    ultimo = Utils.commandExecBash(folder + "check 3.85.105.217");
            //Console.WriteLine("ULTIMO: "+ultimo.Substring(0,3));

            RegionEndpoint regiao = RegionEndpoint.USEast1; //RegionEndpoint.USEast1=Virginia // RegionEndpoint.SAEast1=Sao Paulo
            string status = InstancesManager.Instance_statusCheck(regiao, "i-065477ba579a6aa94");
            if (status.Equals("running"))
                Console.WriteLine("Rodando");
            Console.WriteLine("Status: "+status);
        }
        //public virtual DescribeInstanceStatusResponse DescribeInstanceStatus(DescribeInstanceStatusRequest request);
        public static string status(string ami){
            AmazonEC2Client client = new AmazonEC2Client(RegionEndpoint.USEast1);
            var response = client.DescribeInstanceStatus(new DescribeInstanceStatusRequest {
                InstanceIds = new List<string> {ami}
            });

            List<InstanceStatus> instanceStatuses = response.InstanceStatuses;
            return instanceStatuses.ElementAt(0).InstanceState.Name;
        }
    }
}
