using System;
using System.Collections.Generic;

namespace AWS.Shelf {
    public interface IBackendServices {
        List<IVirtualMachine> VirtualMachines { get; }
        string deploy(string platform_config);
        void undeploy();
    }
    public interface IVirtualMachine {
        string InstanceId { get; set;}
        string PrivateIpAddress { get; set; }
        string PublicIpAddress { get; set; }
    }
}
