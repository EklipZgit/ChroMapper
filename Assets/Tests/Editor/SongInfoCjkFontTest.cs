using System.Collections;
using Beatmap.Info;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class SongInfoCjkFontTest
    {
        // The SongInfo edit fields instantiate this prefab; its text component's font must resolve
        // every script mappers can type into song metadata, through the font's own fallback chain.
        private const string InputFieldPrefabPath = "Assets/_Prefabs/UI/InputField Container.prefab";

        // Header/title labels on the same screen are wired to Teko directly.
        private const string TekoFontPath = "Assets/_Graphics/Materials/Font/Teko.asset";

        // Teko's CJK coverage rides on this dynamic fallback; the song list exercises it at scale.
        private const string NotoSansCjkFontPath = "Assets/_Graphics/Materials/Font/NotoSansCJKjp.asset";

        // Representative codepoints spanning the scripts Beat Saber maps carry in metadata.
        private static readonly uint[] CjkCodepoints =
        {
            0x3042u, // あ hiragana
            0x30ABu, // カ katakana
            0x697Du, // 楽 kanji
            0x4E2Du, // 中 hanzi
            0x6C49u, // 汉 simplified
            0x570Bu, // 國 traditional
            0xD55Cu, // 한 hangul
            0xB178u // 노 hangul
        };

        [Test]
        public void InputFieldTextResolvesCjkCharacters()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(InputFieldPrefabPath);
            Assert.NotNull(prefab, "InputField Container prefab missing");

            var inputField = prefab.GetComponentInChildren<TMP_InputField>(true);
            Assert.NotNull(inputField, "Prefab has no TMP_InputField");
            var font = inputField.textComponent.font;
            Assert.NotNull(font, "Input field text component has no font asset");

            foreach (var unicode in CjkCodepoints)
            {
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(
                    unicode, font, true, FontStyles.Normal, FontWeight.Regular, out _);
                Assert.NotNull(
                    character,
                    $"U+{unicode:X4} does not resolve through {font.name}'s fallback chain; " +
                    "CJK song metadata will render as missing glyphs in the SongInfo fields");
            }
        }

        // SongListCjkNamesKeepResolvingPastAtlasCapacity: the song select screen resolves a fresh
        // glyph per new CJK character it lists, so the dynamic atlas must keep accepting additions —
        // a single near-full atlas with multi-atlas disabled silently drops every later character,
        // which is why CJK map names rendered nothing at all.
        [Test]
        public void DynamicCjkAtlasAcceptsNewGlyphsAtScale()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSansCjkFontPath);
            Assert.NotNull(font, "NotoSansCJKjp font asset missing");

            // U+9F40..U+9FA3 is a populated CJK-unified range absent from the baked glyph table.
            var failures = 0;
            for (uint unicode = 0x9F40; unicode <= 0x9FA3; unicode++)
            {
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(
                    unicode, font, true, FontStyles.Normal, FontWeight.Regular, out _);
                if (character == null) failures++;
            }

            Assert.Zero(
                failures,
                "dynamic CJK atlas stopped accepting new glyphs; song-list names beyond capacity render nothing");
        }

        // The song-list row prefab AssignSong writes to; its Title/Artist/Folder fields use Teko,
        // which resolves CJK through the dynamic NotoSansCJKjp fallback at runtime.
        private const string SongListElementPrefabPath = "Assets/_Prefabs/UI/SongListElement.prefab";

        // End-to-end regression for blank CJK map names in the song list: drives the real prefab
        // through AssignSong with the metadata of the map that reported the failure, then verifies
        // every non-ASCII character resolved to its own glyph — a substitution with the baked
        // missing-glyph char (U+25A1) or a null element means the dynamic atlas dropped it.
        [UnityTest]
        public IEnumerator SongListElementResolvesAllCjkGlyphsInMapMetadata()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SongListElementPrefabPath);
            Assert.NotNull(prefab, "SongListElement prefab missing");

            var go = Object.Instantiate(prefab);
            var item = go.GetComponent<SongListItem>();
            Assert.NotNull(item, "SongListElement has no SongListItem");

            var info = new BaseInfo
            {
                SongName = "どうしても肩にちっちゃい重機を乗せたいお願いマッスル",
                SongSubName = "",
                SongAuthorName = "肩にサラミ乗せてんのかい",
                Directory = "1da52 (どうしても肩にちっちゃい重機を乗せたいお願いマッスル - 肩にサラミ乗せてんのかい)"
            };
            item.AssignSong(info, "");
            yield return null;

            var unresolved = 0;
            foreach (var field in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                field.ForceMeshUpdate();
                foreach (var ci in field.textInfo.characterInfo)
                {
                    if (ci.character <= 0x7F || ci.elementType != TMP_TextElementType.Character) continue;
                    if (ci.textElement == null || ci.textElement.unicode != ci.character)
                    {
                        Debug.LogError($"{field.name}: U+{(int)ci.character:X4} resolved to " +
                                       (ci.textElement == null ? "nothing" : $"U+{ci.textElement.unicode:X4}"));
                        unresolved++;
                    }
                }
            }

            Object.Destroy(go);
            Assert.Zero(unresolved, "song-list fields contain characters with no resolved glyph");
        }

        [Test]
        public void TekoFontResolvesCjkCharacters()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TekoFontPath);
            Assert.NotNull(font, "Teko font asset missing");

            foreach (var unicode in CjkCodepoints)
            {
                var character = TMP_FontAssetUtilities.GetCharacterFromFontAsset(
                    unicode, font, true, FontStyles.Normal, FontWeight.Regular, out _);
                Assert.NotNull(
                    character,
                    $"U+{unicode:X4} does not resolve through Teko's fallback chain");
            }
        }
    }
}
