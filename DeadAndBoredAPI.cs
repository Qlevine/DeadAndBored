using System;
using System.Collections.Generic;
using System.Text;

namespace DeadAndBored
{
    public class DeadAndBoredAPI
    {
        /// <summary>
        /// Is the mod loaded
        /// </summary>
        public static bool IsLoaded => DeadAndBored.DeadAndBoredObject.Instance != null;

        /// <summary>
        /// Is the local player dead and talking as an enemy (while spectating an enemy)
        /// </summary>
        public static bool IsDeadAndTalking => DeadAndBored.DeadAndBoredObject.Instance.isDeadAndTalking;
    }
}
