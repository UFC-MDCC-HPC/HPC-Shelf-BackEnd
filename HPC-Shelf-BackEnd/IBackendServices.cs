using System;
using System.Collections.Generic;

namespace HPCBackendServices {
    public interface IBackendServices {
        string deploy(string platform_config);
    }
    public interface IVirtualMachine {
        string InstanceId { get; set;}
        string PrivateIpAddress { get; set; }
        string PublicIpAddress { get; set; }
    }
}
