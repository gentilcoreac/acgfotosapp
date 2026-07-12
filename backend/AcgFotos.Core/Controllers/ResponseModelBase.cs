using System;
using System.Collections.Generic;

namespace AcgFotos.Core.Controllers
{
    public class ResponseModelBase
    {
        public bool Success { get; set; }

        public long? Id { get; set; }

        public string[] Errors { get; set; }
        public Exception Exception { get; set; }

        public ResponseModelBase()
        {
            this.Success = false;
        }

        public ResponseModelBase(string errorMessage)
        {
            this.Success = false;
            var errors = new List<string> { errorMessage };
            this.Errors = errors.ToArray();
        }
    }
}
