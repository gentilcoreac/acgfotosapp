using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AcgFotos.Base.Application.Identity
{
    public class ConfirmEmailViewModel
    {
        public string UserId { get; set; }
        public string Code { get; set; }
    }
}
