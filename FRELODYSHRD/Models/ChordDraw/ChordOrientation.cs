using System.Runtime.Serialization;

namespace FRELODYSHRD.Models.ChordDraw
{
    public enum ChordOrientation
    {
        [EnumMember(Value = "vertical")] Vertical = 0,
        [EnumMember(Value = "horizontal")] Horizontal = 1
    }
}
