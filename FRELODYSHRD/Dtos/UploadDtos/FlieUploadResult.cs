using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYSHRD.Dtos.UploadDtos
{
    public class FileUploadResult
    {
        public string FilePath { get; set; }
        public string OriginalFileName { get; set; }
        public long Size { get; set; }
        public bool Success { get; set; }
    }
}
