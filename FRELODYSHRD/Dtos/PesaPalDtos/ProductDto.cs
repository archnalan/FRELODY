using FRELODYAPP.Dtos.SubDtos;
using FRELODYSHRD.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.PesaPalDtos
{
    public class ProductDto : BaseEntityDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public BillingPeriod? Period { get; set; }
        public List<Feature>? Features { get; set; }
        public bool? IsPopular { get; set; }
    }
}
