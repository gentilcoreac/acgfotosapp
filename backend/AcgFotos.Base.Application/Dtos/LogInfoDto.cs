using AcgFotos.Core.Application;
using System;

namespace AcgFotos.Base.Application.Dtos
{
    public class LogInfoDto : DtoBase
    {
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Level { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Exception { get; set; }
        public string Properties { get; set; }
    }
}
