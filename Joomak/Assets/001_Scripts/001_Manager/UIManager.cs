using System;
using System.Collections.Generic;
using _001_Scripts._004_UI;
using UnityEngine;

namespace _001_Scripts._001_Manager
{
    // 씬에 있는 패널들의 레지스트리. 패널이 Start에서 스스로 등록한다.
    // 패널 종류를 enum으로 두면 홀/주방이 같은 파일에 항목을 추가하다 충돌하므로 타입으로 구분한다.
    public class UIManager : SinManagerBase<UIManager>
    {
        private readonly Dictionary<Type, PanelBase> panels = new();

        public IReadOnlyCollection<PanelBase> Panels => panels.Values;

        public override void Initialize()
        {
            panels.Clear();
        }

        public void RegisterPanel(PanelBase panel)
        {
            if (panel == null)
            {
                return;
            }

            Type key = panel.GetType();
            if (panels.TryGetValue(key, out PanelBase existing) && existing != null && existing != panel)
            {
                Debug.LogWarning($"[UIManager] {key.Name}이 이미 등록되어 있습니다. 씬에 중복 배치됐는지 확인하세요.", panel);
                return;
            }

            panels[key] = panel;
        }

        public void UnregisterPanel(PanelBase panel)
        {
            if (panel == null)
            {
                return;
            }

            Type key = panel.GetType();
            if (panels.TryGetValue(key, out PanelBase existing) && existing == panel)
            {
                panels.Remove(key);
            }
        }

        public bool TryGetPanel<T>(out T panel) where T : PanelBase
        {
            if (panels.TryGetValue(typeof(T), out PanelBase found) && found != null)
            {
                panel = (T)found;
                return true;
            }

            panel = null;
            return false;
        }

        public T GetPanel<T>() where T : PanelBase => TryGetPanel(out T panel) ? panel : null;

        public bool OpenPanel<T>() where T : PanelBase
        {
            if (!TryGetPanel(out T panel))
            {
                return false;
            }

            panel.Open();
            return true;
        }

        public bool ClosePanel<T>() where T : PanelBase
        {
            if (!TryGetPanel(out T panel))
            {
                return false;
            }

            panel.Close();
            return true;
        }

        public bool TogglePanel<T>() where T : PanelBase
        {
            if (!TryGetPanel(out T panel))
            {
                return false;
            }

            panel.Toggle();
            return true;
        }

        // HUD처럼 늘 떠 있어야 하는 패널까지 닫으면 안 되므로 AlwaysVisible은 건너뛴다.
        public void CloseAll()
        {
            foreach (PanelBase panel in panels.Values)
            {
                if (panel != null && !panel.AlwaysVisible)
                {
                    panel.Close();
                }
            }
        }
    }
}
