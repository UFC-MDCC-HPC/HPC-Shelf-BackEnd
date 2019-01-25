using System;
using System.Threading;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Collections.Generic;

namespace AWS.Shelf { 
    public class Test {
        public static void run() {
            AmazonEC2Client client = new AmazonEC2Client(RegionEndpoint.SAEast1);

            string keyPairName = "credential";
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/";
            InstancesManager.CreateKeyPair(client, keyPairName, home + keyPairName + ".pem");

            string securityGroupName = "hpc-shelf";

            var instanceId = InstancesManager.LaunchInstance("ami-0318cb6e2f90d688b", keyPairName, securityGroupName, client);

            Thread.Sleep(1000);

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
                    Console.Write(runningInstance.InstanceId + " ");
                    Console.Write(runningInstance.InstanceType + " private: ");
                    Console.Write(runningInstance.PrivateIpAddress + " public: ");
                    Console.WriteLine(runningInstance.PublicIpAddress);
                }
            }

            InstancesManager.TerminateInstances(new List<String> { instanceId }, client);
        }
    }
}
