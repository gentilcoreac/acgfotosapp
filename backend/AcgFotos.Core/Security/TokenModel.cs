using System;

namespace AcgFotos.Core.Security
{
    public class TokenModel
    {
        public bool? HasVerifiedEmail { get; set; }

        public string Token { get; set; }
        public DateTime ValidTo { get; set; }

        public long TenantId { get; set; }

        public bool IsRoot { get; set; }
    }
}
