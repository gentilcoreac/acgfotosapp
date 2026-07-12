using System;

namespace AcgFotos.Base.Application.Outputs
{
    public class LogInfoAllOutput
    {
        public long Id { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
        public long TenantId { get; set; }
    }
}
