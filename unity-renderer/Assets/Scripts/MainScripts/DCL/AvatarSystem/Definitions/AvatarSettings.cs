using UnityEngine;

namespace AvatarSystem
{
    public struct AvatarSettings
    {
        /// <summary>
        /// Name of the player controlling this avatar (if any)
        /// 控制这个角色的玩家的名字(如果有的话) 
        /// </summary>
        public string playerName;

        /// <summary>
        /// Bodyshape ID of the avatar
        /// </summary>
        public string bodyshapeId;

        /// <summary>
        /// Hair color of the avatar
        /// </summary>
        public Color hairColor;

        /// <summary>
        /// Skin color of the avatar
        /// </summary>
        public Color skinColor;

        /// <summary>
        /// Eyes color of the avatar
        /// </summary>
        public Color eyesColor;
    }

}