using System;
using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace HPCBackendServices {
    public class InstancesManager {
        public static string LaunchInstance(string amiID, string keyPairName, string secGroupName, AmazonEC2Client client) {
            SecurityGroup mySG = GetSecurityGroup(secGroupName, client);
            List<string> groups = new List<string>() { mySG.GroupId };

            var launchRequest = new RunInstancesRequest() {
                ImageId = amiID,
                InstanceType = InstanceType.T1Micro,
                MinCount = 1,
                MaxCount = 1,
                KeyName = keyPairName,
                SecurityGroupIds = groups
            };

            var launchResponse = client.RunInstances(launchRequest);
            var instances = launchResponse.Reservation.Instances;
            var instanceIds = new List<string>();
            foreach (Instance item in instances) {
                instanceIds.Add(item.InstanceId);
                Console.WriteLine("New instance: " + item.InstanceId);
                Console.WriteLine("Instance state: " + item.State.Name);
            }
            return instanceIds[0];
        }
        public static void StopInstance(string instanceId, AmazonEC2Client client) {
            StopInstances(new List<String> { instanceId }, client);
        }
        public static void StopInstances(List<String> instanceIds, AmazonEC2Client client) {
            var request = new StopInstancesRequest(instanceIds);
            StopInstancesResponse response = client.StopInstances(request);
        }

        public static void TerminateInstances(List<String> instanceIds, AmazonEC2Client ec2Client) {
            foreach (string id in instanceIds)
                TerminateInstance(id, ec2Client);
        }
        public static void TerminateInstance(string instanceId, AmazonEC2Client ec2Client) {
            var request = new TerminateInstancesRequest();
            request.InstanceIds = new List<string>() { instanceId };
            try {
                var response = ec2Client.TerminateInstances(request);
                foreach (InstanceStateChange item in response.TerminatingInstances) {
                    Console.WriteLine("Terminated instance: " + item.InstanceId);
                    Console.WriteLine("Instance state: " + item.CurrentState.Name);
                }
            }
            catch (AmazonEC2Exception ex) {
                if ("InvalidInstanceID.NotFound" == ex.ErrorCode) {
                    Console.WriteLine("Instance {0} does not exist.", instanceId);
                }
                else {
                    throw;
                }
            }
        }
        public static SecurityGroup GetSecurityGroup(string sgName, AmazonEC2Client client) {
            var request = new DescribeSecurityGroupsRequest();
            var response = client.DescribeSecurityGroups(request);
            List<SecurityGroup> mySGs = response.SecurityGroups;

            var sg = mySGs.Find(x => x.GroupName.Equals(sgName));

            //TODO: handle case where groupID not found. Find returns a default type (SecurityGroup) if not found.
            Console.WriteLine("Found security group name equal to {0}. (ID: {1})", sg.GroupName, sg.GroupId);

            return sg;
        }
        public static void CreateKeyPair(AmazonEC2Client ec2Client, string keyPairName, string privateKeyFile) {
            var request = new CreateKeyPairRequest();
            request.KeyName = keyPairName;

            try {
                var response = ec2Client.CreateKeyPair(request);
                Console.WriteLine();
                Console.WriteLine("New key: " + keyPairName);

                using (FileStream s = new FileStream(privateKeyFile, FileMode.Create))
                using (StreamWriter writer = new StreamWriter(s)) {
                    writer.WriteLine(response.KeyPair.KeyMaterial);
                }
            }
            catch (Exception ex) {
                Console.WriteLine("The key pair \"{0}\" already exists.", keyPairName);
            }
        }
        public static string Instance_status(RegionEndpoint regiao, string ami) {
            AmazonEC2Client ec2Client = new AmazonEC2Client(regiao);
            var response = ec2Client.DescribeInstanceStatus(new DescribeInstanceStatusRequest {
                InstanceIds = new List<string> { ami }
            });

            List<InstanceStatus> instanceStatuses = response.InstanceStatuses;
            if(instanceStatuses.Count>0)
                return instanceStatuses.ElementAt(0).InstanceState.Name;
            return "";
        }
        public static string Instance_statusCheck(RegionEndpoint regiao, string ami) {
            AmazonEC2Client ec2Client = new AmazonEC2Client(regiao);
            var response = ec2Client.DescribeInstanceStatus(new DescribeInstanceStatusRequest {
                InstanceIds = new List<string> { ami }
            });

            List<InstanceStatus> instanceStatuses = response.InstanceStatuses;
            if (instanceStatuses.Count > 0)
                return instanceStatuses.ElementAt(0).SystemStatus.Status;
            return "";
        }
    }
}
