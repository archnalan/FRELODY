using System.Runtime.Serialization;

namespace FRELODYSHRD.Models.ChordDraw
{
    public enum ChordStyle
    {
        [EnumMember(Value = "normal")] Normal = 0,
        [EnumMember(Value = "handdrawn")] Handdrawn = 1
    }
}
