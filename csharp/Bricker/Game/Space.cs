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
        Sent = 15,
        Edge = 16   // formally 8
    }

    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class SpaceExtension
    {
        /// <summary>
        /// Returns true if space is solid (1-7, 15, or 16).
        /// </summary>
        public static bool IsSolid(this Space s)
        {
            return ((s >= Space.I_White) && (s <= Space.Z_Red)) || (s == Space.Edge) || (s == Space.Sent);
        }

        /// <summary>
        /// Returns true if space is standard (1-7).
        /// </summary>
        public static bool IsStandard(this Space s)
        {
            return (s >= Space.I_White) && (s <= Space.Z_Red);
        }

        /// <summary>
        /// Returns true if space is ghost (1-7).
        /// </summary>
        public static bool IsGhost(this Space s)
        {
            return (s >= Space.I_White_Ghost) && (s <= Space.Z_Red_Ghost);
        }
    }
}
