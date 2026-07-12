using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AcgFotos.Core.Exceptions
{
    public class BusinessValidationException : BusinessException
    {

        public IList<ValidationFailure> Errors { get; private set; }

        public BusinessValidationException(string message) : base(message)
        {
            this.Errors = new List<ValidationFailure>();
            this.Errors.Add(new ValidationFailure("", message));
        }
        public BusinessValidationException(string message, List<string> messages) : base(message)
        {
            if (messages.Count > 0)
            {
                this.Errors = new List<ValidationFailure>();
                foreach (var mensaje in messages)
                {
                    this.Errors.Add(new ValidationFailure("", mensaje));
                }
            }
        }

        public BusinessValidationException(string message, IList<ValidationFailure> errors) : base(message)
        {
            this.Errors = errors;
        }
    }
}
