using AcgFotos.Core.Domain;
using System;

namespace AcgFotos.Base.Domain.Entities {
    public class LogInfo : MultiTenantEntityBase {
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
    }
}
