namespace FRELODYSHRD.Models.ChordDraw
{
    public enum ChordCellState
    {
        Inactive = 0,
        Active = 1,
        Left = 2,
        Middle = 3,
        Right = 4,
        LeftHighlight = 5,
        MiddleHighlight = 6,
        RightHighlight = 7
    }

    public enum ChordEmptyStringState
    {
        Open = 0,
        Muted = 1,
        NotEmpty = 2
    }
}
