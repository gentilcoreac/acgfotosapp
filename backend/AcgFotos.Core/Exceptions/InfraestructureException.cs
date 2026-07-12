using System;

namespace AcgFotos.Core.Exceptions {
    public class InfraestructureException : Exception {
        public InfraestructureException(string message) : base(message) {

        }
        public InfraestructureException(
            string message, 
            Exception innerException) : base(message, innerException) {

        }
    }
}
