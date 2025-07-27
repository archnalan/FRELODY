using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYLIB.ServiceHandler
{
    public class PaginationDetails<T>
    {
        public int OffSet { get; set; }
        public int Limit { get; set; }
        public int TotalSize { get; set; }
        public bool HasMore { get; set; }
        public List<T>? Data { get; set; }
    }
}
