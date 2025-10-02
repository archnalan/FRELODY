using FRELODYSHRD.Dtos.CreateDtos;
using FRELODYUI.Shared.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Services
{
    public class TabManagementService
    {
        public List<TabItem> TabItems { get; set; } = new();        
        public TabItem? ActiveTab { get; set; }

        public event Func<RelocateLineActionDto, Task>? OnRelocateLine;
        public event Func<int, int, Task>? OnRemoveEmptyLine;
        public event Func<int, int, List<SegmentCreateDto>?, Task>? OnAddLineToSection;
        public async Task RelocateLine(RelocateLineActionDto relocateRequest)
        {
            if (OnRelocateLine != null)
            {
                await OnRelocateLine(relocateRequest);
            }
        }
        public async Task RemoveEmptyLine(int sectionId, int lineNumber)
        {
            if (OnRemoveEmptyLine != null)
            {
                await OnRemoveEmptyLine(sectionId, lineNumber);
            }
        }
        public async Task AddLineToSection(int sectionId, int afterLine = 0, List<SegmentCreateDto>? content = null)
        {
            if (OnAddLineToSection != null)
            {
                await OnAddLineToSection(sectionId, afterLine, content);
            }
        }
    }
}
