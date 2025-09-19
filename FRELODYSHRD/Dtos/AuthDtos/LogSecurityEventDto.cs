using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.AuthDtos
{

    public class LogSecurityEventDto
    {
        public string UserId { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
    }
}
