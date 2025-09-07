using FRELODYSHRD.Dtos.UploadDtos;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.RefitApis
{
    public interface IFileUploadApi
    {
        [Multipart]
        [Post("/api/file-upload/upload-chart-image")]
        Task<IApiResponse<FileUploadResult>> UploadChartImage([AliasAs("file")] StreamPart file);

        [Multipart]
        [Post("/api/file-upload/upload-chart-audio")]
        Task<IApiResponse<FileUploadResult>> UploadChartAudio([AliasAs("file")] StreamPart file);
    }
}
