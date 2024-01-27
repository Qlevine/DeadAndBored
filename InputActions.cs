using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace DeadAndBored
{
    public class InputActions : LcInputActions
    {
        [InputAction("<Keyboard>/y", Name = "Key To Talk")]
        public InputAction TalkKey { get; set; }

        //[InputAction("<Keyboard>/y", Name = "Key To Talk")]
        //public InputAction TalkKey { get; set; }

        [InputAction("<Keyboard>/u", Name = "Reset Audio Data")]
        public InputAction ResetAudio { get; set; }

    }


    //deadAndTalkingKeyConfig = config.Bind("Config", "Key To Talk", KeyCode.Y, "Key press to talk as enemy (when spectating enemy).");
    //        enableTooltipConfig = config.Bind("Config", "Enable Tooltip", true, "Enable the tooltip menu");
    //        hearOtherTeammates = config.Bind("Config", "Hear Other Dead Teammates While They Are Talking As An Enemy", false, "With this set to False, you will not hear your other dead teammates when they are talking as an enemy. You will hear them again when they stop talking as an enemy. Note there is a delay so you may still hear/not hear them for a brief period of time.");
    //        enableLogging = config.Bind("Config", "Enable Debug Logging", false, "Set to true for debugging");
    //        manuallyResetAudioDataConfig = config.Bind("Config", "If a player loses audio, they can press this key to reset their audio data.", KeyCode.U, "Key press to manually reset audio data");

}
