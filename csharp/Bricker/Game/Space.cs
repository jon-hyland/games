namespace Bricker.Game
{
    /// <summary>
    /// Represents the contents of a grid space.
    /// </summary>
    public enum Space : byte
    {
        Empty = 0,
        I_White = 1,
        J_Blue = 2,
        L_Yellow = 3,
        O_Gray = 4,
        S_Green = 5,
        T_Purple = 6,
        Z_Red = 7,
        I_White_Ghost = 8,
        J_Blue_Ghost = 9,
        L_Yellow_Ghost = 10,
        O_Gray_Ghost = 11,
        S_Green_Ghost = 12,
        T_Purple_Ghost = 13,
        Z_Red_Ghost = 14,
        Edge = 15   // formally 8
    }
}
