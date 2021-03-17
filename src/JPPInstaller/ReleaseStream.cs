using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace JPPInstaller
{
    public class ReleaseStream
    {
        public string Name { get; set; }
        public ReleaseClass Class { get; set; }
        
        public Uri BaseUrl { get; set; }
        
        public long ReleaseId { get; set; }
    }

    public enum ReleaseClass
    {
        Alpha,
        Beta,
        Release
    }
}
