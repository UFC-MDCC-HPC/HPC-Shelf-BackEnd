﻿using System.Web;
using System.Web.Http;

namespace br.ufc.mdcc.hpcshelf.backend {
    public class Global : HttpApplication {
        protected void Application_Start() {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
