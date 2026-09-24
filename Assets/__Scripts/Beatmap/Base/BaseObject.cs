using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base.Customs;
using Beatmap.Enums;
using LiteNetLib.Utils;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    public abstract class BaseObject : BaseItem,
        ICustomData,
        IHeckObject,
        IChromaObject,
        INetSerializable,
        IComparable<BaseObject>
    {
        public virtual void Serialize(NetDataWriter writer)
        {
            writer.Put(jsonTime);
            writer.Put((float)songBpmTime);
            writer.Put(CustomData?.ToString());
        }

        public virtual void Deserialize(NetDataReader reader)
        {
            jsonTime = reader.GetFloat();
            songBpmTime = reader.GetFloat();
            CustomData = JSON.Parse(reader.GetString());
        }

        protected BaseObject()
        {
            SetMap();
            JsonTime = 0; // needed to set songBpmTime
        }

        protected BaseObject(float time, JSONNode customData = null)
        {
            SetMap();
            JsonTime = time;
            CustomData = customData;
        }

        protected BaseObject(float jsonTime, float songBpmTime, JSONNode customData = null)
        {
            SetMap();
            this.jsonTime = jsonTime;
            this.songBpmTime = songBpmTime;
            CustomData = customData;
        }

        public virtual void SetMap(BaseDifficulty map = null)
        {
            if (map != null)
            {
                Map = map;
            }
            else
            {
                Map = BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Map : null;
            }
        }

        public abstract ObjectType ObjectType { get; set; }
        public bool HasAttachedContainer { get; set; }

        protected BaseDifficulty Map;

        protected float jsonTime;

        public float JsonTime
        {
            get => jsonTime;
            set
            {
                jsonTime = value;
                RecomputeSongBpmTime();
            }
        }

        // should only be set directly when initializing
        // read from SongBpmTime instead, and write to JsonTime to update this
        // really should be private but we need to set this from BaseDifficulty on init
        // TODO: this is not song BPM time, it's grid or timeline position
        internal float? songBpmTime;

        // Position of this object within its source file's arrays, assigned during difficulty
        // parse. Chroma resolves same-time duplicates in file order (last wins), so saved
        // output must reproduce authored ties; runtime-created objects keep MaxValue and
        // interleave chronologically via JsonTime instead.
        internal int FileOrder = int.MaxValue;
        public float SongBpmTime => (float)songBpmTime;

        public virtual Color? CustomColor { get; set; }
        public abstract string CustomKeyColor { get; }

        private JSONNode customData = new JSONObject();

        public JSONNode CustomData
        {
            get => customData;
            set
            {
                customData = value ?? new JSONObject();
                ParseCustom();
            }
        }

        public void SetCustomData(JSONNode node) => customData = node ?? new JSONObject();

        public virtual bool IsChroma() => false;

        public virtual bool IsNoodleExtensions() => false;

        public virtual bool IsMappingExtensions() => false;

        public JSONNode CustomTrack { get; set; }

        public abstract string CustomKeyTrack { get; }

        public virtual void RecomputeSongBpmTime() => songBpmTime = Map?.JsonTimeToSongBpmTime(JsonTime);

        public virtual bool IsConflictingWith(BaseObject other, bool deletion = false) =>
            Mathf.Abs(JsonTime - other.JsonTime) < BeatmapObjectContainerCollection.Epsilon
            && IsConflictingWithObjectAtSameTime(other, deletion);

        protected abstract bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false);

        public virtual bool HasMatchingTrack(string filter) =>
            (filter == null)
            || CustomTrack switch
            {
                JSONString str => filter == (string)str,
                JSONArray arr => arr.Children.Any((it) => filter == (string)it),
                _ => false,
            };

        public virtual void Apply(BaseObject originalData)
        {
            JsonTime = originalData.JsonTime;
            CustomData = originalData.CustomData?.Clone();
        }

        protected virtual void ParseCustom()
        {
            CustomTrack = (CustomData?.HasKey(CustomKeyTrack) ?? false) ? CustomData?[CustomKeyTrack] : null;
            CustomColor = (CustomData?.HasKey(CustomKeyColor) ?? false)
                ? CustomData?[CustomKeyColor].ReadColor()
                : null;
        }

        public void RefreshCustom() => ParseCustom();

        protected internal virtual JSONNode SaveCustom()
        {
            var node = CustomData is JSONObject ? CustomData : new JSONObject();
            if (CustomTrack != null)
                node[CustomKeyTrack] = CustomTrack;
            else
                node.Remove(CustomKeyTrack);
            if (CustomColor != null)
                // Keep opaque custom colors compact; Chroma treats an omitted alpha as fully opaque.
                node[CustomKeyColor] = new JSONArray().WriteColor(CustomColor.Value, CustomColor.Value.a != 1f);
            else
                node.Remove(CustomKeyColor);

            SetCustomData(node);
            return node;
        }

        public void WriteCustom() => SaveCustom();

        public JSONNode GetOrCreateCustom()
        {
            if (CustomData == null) CustomData = new JSONObject();

            return CustomData;
        }

        // Generic comparison function that only cares about time
        public virtual int CompareTo(BaseObject other) => JsonTime.CompareTo(other.JsonTime);

        // Order objects for serialization: chronological, with authored file order preserved
        // for same-time entries (Chroma's last-wins resolution depends on it — sorting
        // _customEvents/_events by time alone scrambled beat-0 hide/show pairs and hid the
        // runway in As The World Caves In). New objects default FileOrder to MaxValue so
        // they append at the tail of their same-time group.
        internal static IEnumerable<T> InFileOrder<T>(IEnumerable<T> objects) where T : BaseObject =>
            objects.OrderBy(o => o.JsonTime).ThenBy(o => o.FileOrder);
    }
}
