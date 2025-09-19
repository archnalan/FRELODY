using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.AuthDtos
{
    public class RefreshTokenDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

    }
}
