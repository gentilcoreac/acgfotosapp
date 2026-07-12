using System;
using System.Collections.Generic;

namespace AcgFotos.Core.Exceptions {
    public class BusinessUpdateException : BusinessException
    {

        public IList<string> Errors { get; private set; }

        public BusinessUpdateException(string message) : base(message) {
            this.Errors = new List<String>();
            this.Errors.Add( message);
        }

        public BusinessUpdateException(string message, IList<string> errors) : base(message) {
            this.Errors = errors;
        }
    }
}
