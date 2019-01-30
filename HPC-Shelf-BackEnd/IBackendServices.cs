using System;
using System.Collections.Generic;

namespace HPCBackendServices {
    public interface IBackendServices {
        List<IVirtualMachine> VirtualMachines { get; }
        string deploy(string platform_config);
    }
    public interface IVirtualMachine {
        string InstanceId { get; set;}
        string PrivateIpAddress { get; set; }
        string PublicIpAddress { get; set; }
    }
}
