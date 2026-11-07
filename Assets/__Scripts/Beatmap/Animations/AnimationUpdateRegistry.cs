using System;
using System.Collections.Generic;

namespace Beatmap.Animations
{
    internal sealed class AnimationUpdateRegistry<T> where T : class
    {
        private struct Entry
        {
            internal T Target;
            internal bool Active;
        }

        private readonly List<Entry> entries = new();
        private readonly Func<T, int> getIndex;
        private readonly Action<T, int> setIndex;
        private bool needsCompaction;

        internal AnimationUpdateRegistry(Func<T, int> getIndex, Action<T, int> setIndex)
        {
            this.getIndex = getIndex;
            this.setIndex = setIndex;
        }

        internal void Register(T target)
        {
            if (getIndex(target) >= 0)
            {
                return;
            }
            setIndex(target, entries.Count);
            entries.Add(new Entry { Target = target, Active = true });
        }

        internal void Unregister(T target)
        {
            var index = getIndex(target);
            if (index < 0)
            {
                return;
            }
            entries[index] = default;
            setIndex(target, -1);
            needsCompaction = true;
        }

        internal void Run(Action<T> update)
        {
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (entry.Active)
                {
                    update(entry.Target);
                }
            }

            Compact();
        }

        internal void Compact()
        {
            if (!needsCompaction)
            {
                return;
            }

            // Recycling an object can disable another animator during this loop. Remove those slots
            // afterwards without changing the order in which the remaining tracks contribute values.
            var count = 0;
            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!entry.Active)
                {
                    continue;
                }
                entries[count] = entry;
                setIndex(entry.Target, count++);
            }
            entries.RemoveRange(count, entries.Count - count);
            needsCompaction = false;
        }
    }
}
