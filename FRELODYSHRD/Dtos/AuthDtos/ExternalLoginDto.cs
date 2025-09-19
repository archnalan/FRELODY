using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.AuthDtos
{
    public class ExternalLoginDto
    {
        public string Code { get; set; }
        public string TenantId { get; set; }
    }
}
