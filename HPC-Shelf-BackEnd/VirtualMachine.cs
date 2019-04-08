using System;
namespace br.ufc.mdcc.hpcshelf.backend
{
	public interface IVirtualMachine
	{
		string InstanceId { get; set; }
		string PrivateIpAddress { get; set; }
		string PublicIpAddress { get; set; }
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
