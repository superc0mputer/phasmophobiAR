using System.Collections;
using UnityEngine;

namespace PhasmophobiAR.Ghosts
{
    /// <summary>Base class for modular scares scheduled by <see cref="HorrorDirector"/>.</summary>
    public abstract class HorrorEvent : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float m_MinimumTension = 0.2f;
        [SerializeField, Min(0.01f)] float m_SelectionWeight = 1f;
        [SerializeField, Min(0f)] float m_CooldownSeconds = 20f;
        [SerializeField, Min(0)] int m_MaxTriggersPerInvestigation = 3;

        float m_NextAllowedTime;
        int m_TriggerCount;

        public float SelectionWeight => Mathf.Max(0.01f, m_SelectionWeight);
        internal virtual bool WantsImmediateTrigger(HorrorDirector director) => false;

        internal virtual bool CanTrigger(HorrorDirector director)
        {
            return enabled
                && gameObject.activeInHierarchy
                && director.Tension >= m_MinimumTension
                && Time.unscaledTime >= m_NextAllowedTime
                && (m_MaxTriggersPerInvestigation <= 0 || m_TriggerCount < m_MaxTriggersPerInvestigation);
        }

        internal IEnumerator Trigger(HorrorDirector director)
        {
            m_TriggerCount++;
            m_NextAllowedTime = Time.unscaledTime + Mathf.Max(0f, m_CooldownSeconds);
            yield return Play(director);
        }

        internal void ResetForInvestigation()
        {
            m_TriggerCount = 0;
            m_NextAllowedTime = 0f;
            OnReset();
        }

        internal void Cancel() => OnCancel();

        protected virtual void OnReset() { }
        protected virtual void OnCancel() { }
        protected abstract IEnumerator Play(HorrorDirector director);
    }
}
