
using System;

namespace AcgFotos.Core.Security
{
    public class TokenModelDto
    {
        public bool? HasVerifiedEmail { get; set; }

        public string Token { get; set; }
        public DateTime ValidTo { get; set; }

        public long TenantId { get; set; }
    }
}
