// Unity 5.6 / C# 4.0
using System;
using System.Collections.Generic;

namespace Packages.BMG.EventDebugger
{
    public static class TrackedEventRepository
    {
        /// <summary>Posted-by events currently in the buffer.</summary>
        public static long NumberOfPostedEvents { get { return s_numberOfPostedEvents; } }

        /// <summary>Monotonic version increments on any mutation.</summary>
        public static ulong Version { get { return s_version; } }

        /// <summary>Current number of stored events (0..MaxCapacity).</summary>
        public static int Count { get { lock (s_lock) { return s_count; } } }

        /// <summary>Raised after Add/Clear completes.</summary>
        public static event Action Changed;

        // ---- Ring buffer storage ----
        private static readonly object s_lock = new object();

        private static readonly TrackedEvent[] s_buffer = new TrackedEvent[Settings.Events.Repository.MaxCapacity];
        private static int s_head;  // index of oldest element (valid if s_count > 0)
        private static int s_tail;  // index where next Add will write
        private static int s_count; // number of valid elements in buffer

        private static long  s_numberOfPostedEvents;
        private static ulong s_version;

        /// <summary>
        /// Add an event; overwrites oldest when buffer is full (O(1)).
        /// </summary>
        public static void Add(TrackedEvent pe)
        {
            if (pe == null) { return; }

            TrackedEvent overwritten = null;

            lock (s_lock)
            {
                // If we are full, we will overwrite the oldest (at head)
                if (s_count == Settings.Events.Repository.MaxCapacity)
                {
                    overwritten = s_buffer[s_tail]; // <-- we’ll overwrite at tail, which is the oldest when full
                    // NOTE: In a canonical ring, when full, head == tail (oldest == next write).
                    // We keep it simple: treat the slot at tail as the one to be overwritten.
                    // Advance head because the oldest is being dropped.
                    if (overwritten == null)
                    {
                        // Fallback if not previously set (shouldn’t happen with stable usage)
                        overwritten = s_buffer[s_head];
                    }
                    s_head = (s_head + 1) % Settings.Events.Repository.MaxCapacity;
                }
                else
                {
                    s_count++;
                }

                // Adjust posted count if we are overwriting one that was PostedBy
                if (overwritten != null && overwritten.Action == EventAction.PostedBy)
                {
                    s_numberOfPostedEvents--;
                }

                // Write new element at tail
                s_buffer[s_tail] = pe;
                if (pe.Action == EventAction.PostedBy) { s_numberOfPostedEvents++; }

                // Advance tail
                s_tail = (s_tail + 1) % Settings.Events.Repository.MaxCapacity;

                s_version++;
            }

            RaiseChanged();
        }

        /// <summary>
        /// Clears the repository (O(1)); does not deallocate the buffer.
        /// </summary>
        public static void Clear()
        {
            lock (s_lock)
            {
                // Zero references so GC can collect old events (optional but safe)
                if (s_count > 0)
                {
                    // Only clear the valid range to avoid a full-array pass when nearly empty
                    for (int i = 0; i < s_count; i++)
                    {
                        int idx = (s_head + i) % Settings.Events.Repository.MaxCapacity;
                        s_buffer[idx] = null;
                    }
                }

                s_head = 0;
                s_tail = 0;
                s_count = 0;
                s_numberOfPostedEvents = 0;
                s_version = 0;
            }

            RaiseChanged();
        }

        /// <summary>
        /// Copies the current contents into <paramref name="dst"/> in oldest→newest order.
        /// Uses caller-provided list to avoid allocations; clears <paramref name="dst"/> first.
        /// </summary>
        public static void FillSnapshot(List<TrackedEvent> dst)
        {
            if (dst == null) { return; }

            lock (s_lock)
            {
                dst.Clear();
                if (s_count == 0) { return;}

                // Ensure capacity to prevent internal growth re-allocations
                if (dst.Capacity < s_count) { dst.Capacity = s_count; }

                for (int i = 0; i < s_count; i++)
                {
                    int idx = (s_head + i) % Settings.Events.Repository.MaxCapacity;
                    dst.Add(s_buffer[idx]);
                }
            }
        }

        private static void RaiseChanged()
        {
            Action cb = Changed;
            if (cb != null) { cb(); }
        }
    }
}
