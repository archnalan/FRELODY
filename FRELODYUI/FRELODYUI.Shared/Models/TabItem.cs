using FRELODYSHRD.ModelTypes;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FRELODYUI.Shared.Models
{
    public class TabItem
    {
        public int Id { get; set; }
        public string SectionName { get; set; } = nameof(SongSection.Verse); // Default to Verse
        public int SectionNumber { get; set; }
        public int DisplayOrder { get; set; }
        public RenderFragment Content { get; set; } = default!;
        public SongSection SectionEnumValue => Enum.Parse<SongSection>(SectionName);
    }
}
