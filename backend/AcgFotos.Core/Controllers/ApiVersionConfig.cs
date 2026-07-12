using System;

namespace AcgFotos.Core.Controllers
{
    public  class ApiVersionConfig
    {
        public int Major { get; set; }

        public int Minor { get; set; }

        public int Patch { get; set; }

        public bool IsTestEnviroment { get; set; }

        public string GetApiVersion()
        {
            //ejemplo resultado: 2.3.8
            return $"{this.Major}.{this.Minor}.{this.Patch}";
        }
    }
}
