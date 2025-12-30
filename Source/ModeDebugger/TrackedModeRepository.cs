// Unity 5.6 / C# 4.0
using System;
using System.Collections.Generic;
using Multimorphic.NetProcMachine.Machine;

namespace Packages.BMG.ModeDebugger
{
    public static class TrackedModeRepository
    {
        private static readonly Dictionary<Mode, TrackedMode> s_modeMap = new Dictionary<Mode, TrackedMode>();

        // Always-maintained, sorted view (Priority desc, then ClassName asc)
        private static readonly List<TrackedMode> s_sortedModes = new List<TrackedMode>();

        private static readonly object s_lock = new object();

        private static ulong s_version;

        public static event Action Changed;

        public static ulong Version
        {
            get { return s_version; }
        }

        /// <summary>Number of tracked modes.</summary>
        public static int Count
        {
            get { lock (s_lock) { return s_sortedModes.Count; } }
        }

        /// <summary>
        /// Legacy snapshot that allocates a new List each call.
        /// Prefer <see cref="FillSnapshot"/> in hot paths.
        /// </summary>
        public static List<TrackedMode> Modes
        {
            get
            {
                lock (s_lock)
                {
                    return new List<TrackedMode>(s_sortedModes);
                }
            }
        }

        /// <summary>
        /// Allocation-free snapshot into caller-provided list.
        /// Preserves repository sort order (Priority desc, then ClassName asc).
        /// </summary>
        public static void FillSnapshot(List<TrackedMode> dst)
        {
            if (dst == null) {return;}

            lock (s_lock)
            {
                dst.Clear();
                int n = s_sortedModes.Count;
                if (n == 0) {return;}

                // Ensure capacity to avoid internal reallocations in dst
                if (dst.Capacity < n) {dst.Capacity = n;}

                for (int i = 0; i < n; i++)
                {
                    dst.Add(s_sortedModes[i]);
                }
            }
        }

        public static void Clear()
        {
            Action handler;
            lock (s_lock)
            {
                if (s_modeMap.Count <= 0) {return;}

                s_modeMap.Clear();
                s_sortedModes.Clear();
                s_version = 0;
                handler = Changed;
            }
            // Fire outside lock
            if (handler != null) {handler();}
        }

        public static void Add(Mode mode, string addedBy = "")
        {
            if (mode == null) {return;}

            Action handler;
            lock (s_lock)
            {
                if (s_modeMap.ContainsKey(mode)) {return;}

                TrackedMode tm = new TrackedMode(mode, addedBy);
                s_modeMap[mode] = tm;

                // Insert in sorted position (Priority desc, then ClassName asc)
                int idx = BinarySearchSorted(tm);
                if (idx < 0) {idx = ~idx;}
                s_sortedModes.Insert(idx, tm);

                s_version++;
                handler = Changed;
            }
            if (handler != null) {handler();}
        }

        public static void SetEnableFlag(Mode mode, bool flag, string parentMode = "")
        {
            if (mode == null) {return;}

            Action handler = null;
            lock (s_lock)
            {
                TrackedMode tm;
                if (!s_modeMap.TryGetValue(mode, out tm))
                {
                    // Add (sorted insert) if missing
                    tm = new TrackedMode(mode, parentMode);
                    s_modeMap[mode] = tm;

                    int idx = BinarySearchSorted(tm);
                    if (idx < 0) {idx = ~idx;}
                    s_sortedModes.Insert(idx, tm);

                    s_version++;
                    handler = Changed;
                }

                if (tm.IsOn == flag) {return;}

                tm.IsOn = flag;
                // Sorting does not depend on IsOn, so no re-sort needed.
                s_version++;
                handler = handler ?? Changed;
            }
            if (handler != null) {handler();}
        }

        /// <summary>
        /// Binary search using the same comparison (Priority desc, ClassName asc).
        /// Returns index if found, else bitwise complement of insertion index.
        /// </summary>
        private static int BinarySearchSorted(TrackedMode value)
        {
            int lo = 0;
            int hi = s_sortedModes.Count - 1;

            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                int cmp = Compare(value, s_sortedModes[mid]);
                if (cmp == 0) {return mid;}
                if (cmp > 0) {lo = mid + 1;}
                else {hi = mid - 1;}
            }
            return ~lo;
        }

        // Compare: Priority desc, then ClassName asc (Ordinal)
        private static int Compare(TrackedMode a, TrackedMode b)
        {
            if (ReferenceEquals(a, b)) {return 0;}
            if (a == null) {return 1;}
            if (b == null) {return -1;}

            if (a.Priority != b.Priority)
            {
                return b.Priority.CompareTo(a.Priority); // higher first
            }

            string ida = a.ClassName ?? string.Empty;
            string idb = b.ClassName ?? string.Empty;
            return string.Compare(ida, idb, StringComparison.Ordinal);
        }
    }
}
