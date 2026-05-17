using System.Runtime.Serialization;

namespace FRELODYSHRD.Models.ChordDraw
{
    public enum ChordShape
    {
        [EnumMember(Value = "circle")] Circle = 0,
        [EnumMember(Value = "square")] Square = 1,
        [EnumMember(Value = "triangle")] Triangle = 2,
        [EnumMember(Value = "pentagon")] Pentagon = 3
    }
}
