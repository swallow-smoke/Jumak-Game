using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _001_Scripts._002_Controller
{
    public enum PlayerControlProfile
    {
        Hall,
        Kitchen
    }

    public enum PlayerControlAction
    {
        MoveUp,
        MoveDown,
        MoveLeft,
        MoveRight,
        Interact,
        SelectUp,
        SelectDown,
        Dash
    }

    public static class PlayerControlBindings
    {
        private const string Prefix = "Controls.";

        public static event Action<PlayerControlProfile> Changed;

        public static Key Get(PlayerControlProfile profile, PlayerControlAction action)
        {
            string key = BuildPreferenceKey(profile, action);
            Key fallback = GetDefault(profile, action);
            int saved = PlayerPrefs.GetInt(key, (int)fallback);
            return Enum.IsDefined(typeof(Key), saved) ? (Key)saved : fallback;
        }

        public static bool TrySet(PlayerControlProfile profile, PlayerControlAction action, Key key, out string error)
        {
            if (key is Key.None or Key.Escape)
            {
                error = "ESC와 빈 키는 지정할 수 없습니다.";
                return false;
            }

            PlayerPrefs.SetInt(BuildPreferenceKey(profile, action), (int)key);
            PlayerPrefs.Save();
            error = string.Empty;
            Changed?.Invoke(profile);
            return true;
        }

        public static void Reset(PlayerControlProfile profile)
        {
            foreach (PlayerControlAction action in Enum.GetValues(typeof(PlayerControlAction)))
            {
                PlayerPrefs.DeleteKey(BuildPreferenceKey(profile, action));
            }

            PlayerPrefs.Save();
            Changed?.Invoke(profile);
        }

        public static string GetProfileLabel(PlayerControlProfile profile) =>
            profile == PlayerControlProfile.Hall ? "홀 담당" : "주방 담당";

        public static string GetActionLabel(PlayerControlAction action) => action switch
        {
            PlayerControlAction.MoveUp => "위로 이동",
            PlayerControlAction.MoveDown => "아래로 이동",
            PlayerControlAction.MoveLeft => "왼쪽 이동",
            PlayerControlAction.MoveRight => "오른쪽 이동",
            PlayerControlAction.Interact => "상호작용",
            PlayerControlAction.SelectUp => "선택 위",
            PlayerControlAction.SelectDown => "선택 아래",
            PlayerControlAction.Dash => "대시",
            _ => action.ToString()
        };

        public static string GetKeyLabel(Key key) => key switch
        {
            Key.Space => "SPACE",
            Key.Enter => "ENTER",
            Key.NumpadEnter => "NUM ENTER",
            Key.LeftShift => "L SHIFT",
            Key.RightShift => "R SHIFT",
            Key.UpArrow => "↑",
            Key.DownArrow => "↓",
            Key.LeftArrow => "←",
            Key.RightArrow => "→",
            _ => key.ToString().ToUpperInvariant()
        };

        private static string BuildPreferenceKey(PlayerControlProfile profile, PlayerControlAction action) =>
            $"{Prefix}{profile}.{action}";

        private static Key GetDefault(PlayerControlProfile profile, PlayerControlAction action)
        {
            if (profile == PlayerControlProfile.Hall)
            {
                return action switch
                {
                    PlayerControlAction.MoveUp => Key.W,
                    PlayerControlAction.MoveDown => Key.S,
                    PlayerControlAction.MoveLeft => Key.A,
                    PlayerControlAction.MoveRight => Key.D,
                    PlayerControlAction.Interact => Key.E,
                    PlayerControlAction.SelectUp => Key.PageUp,
                    PlayerControlAction.SelectDown => Key.PageDown,
                    PlayerControlAction.Dash => Key.LeftShift,
                    _ => Key.None
                };
            }

            return action switch
            {
                PlayerControlAction.MoveUp => Key.UpArrow,
                PlayerControlAction.MoveDown => Key.DownArrow,
                PlayerControlAction.MoveLeft => Key.LeftArrow,
                PlayerControlAction.MoveRight => Key.RightArrow,
                PlayerControlAction.Interact => Key.Space,
                PlayerControlAction.SelectUp => Key.UpArrow,
                PlayerControlAction.SelectDown => Key.DownArrow,
                PlayerControlAction.Dash => Key.RightShift,
                _ => Key.None
            };
        }
    }
}
