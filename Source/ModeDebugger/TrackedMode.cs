// Unity 5.6 / C# 4.0
using System;
using Multimorphic.NetProcMachine.Machine;

namespace Packages.BMG.ModeDebugger
{
    [System.Serializable]
    public class TrackedMode
    {
        public Mode P3Mode { get { return _p3Mode; } }
        public string ClassName { get { return _className; } }
        public string FullName { get { return _fullName; } }
        public string AddedBy { get { return _addedBy; } }
        public int Priority { get { return _priority; } }

        public bool IsOn
        {
            get
            {
                return _isOn;
            }
            set
            {
                if (!_isOn)
                {
                    if (value)
                    {
                        // Start the age timer now
                        _createdAtUtc = DateTime.UtcNow;
                        _aged = false;
                    }
                    else
                    {
                        _aged = true;
                    }
                }
                _isOn = value;
            }
        }

        /// <summary>
        /// Normalized age in [0,1]. 0 at creation time, 1 at >= 5 seconds.
        /// Also flips the internal _aged flag once the duration elapses.
        /// </summary>
        public float Age
        {
            get
            {
                UpdateAge(); // keeps _aged in sync
                double seconds = (DateTime.UtcNow - _createdAtUtc).TotalSeconds;
                if (seconds <= 0.0) {return 0f;}
                if (seconds >= _ageDurationSeconds) {return 1f;}
                return (float)(seconds / _ageDurationSeconds);
            }
        }

        /// <summary>
        /// Optional: expose whether the mode has aged past the threshold.
        /// </summary>
        public bool IsAged
        {
            get
            {
                UpdateAge();
                return _aged;
            }
        }

        private readonly Mode _p3Mode;
        private readonly string _className;
        private readonly string _fullName;
        private readonly string _addedBy;
        private int _priority;
        private bool _isOn;

        // Aging
        private DateTime _createdAtUtc;
        private bool _aged;
        private const double _ageDurationSeconds = 5.0;

        private const string _description_delimiter = "  pri=";

        public TrackedMode(Mode mode, string addedBy = "")
        {
            string desc = mode.ToString();
            _p3Mode = mode;
            _priority = mode.Priority;
            Type t = mode.GetType();
            _fullName = t.FullName;

            // NOTE: assumes the delimiter exists; adjust if needed for safety.
            int idx = desc.IndexOf(_description_delimiter, StringComparison.Ordinal);
            _className = (idx > 0) ? desc.Substring(0, idx - 1) : desc;

            _addedBy = addedBy;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(AddedBy))
            {
                return string.Format("{0}.{1}_{2}", AddedBy, ClassName, Priority);
            }
            else
            {
                return string.Format("{0}_{1}", ClassName, Priority);
            }
        }

        private void UpdateAge()
        {
            if (_aged) {return;}
            double seconds = (DateTime.UtcNow - _createdAtUtc).TotalSeconds;
            if (seconds >= _ageDurationSeconds)
            {
                _aged = true;
            }
        }
    }
}