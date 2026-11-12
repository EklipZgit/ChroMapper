using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace Beatmap.Appearances
{
    internal sealed class NativeShaderVariants
    {
        private const string VariantsKey = "variants";
        private const string RuntimeKeywordsKey = "runtimeKeywords";
        private readonly string[][] variants;
        private readonly HashSet<string> runtimeKeywords;
        private readonly Dictionary<string, string[]> resolvedRequests = new(StringComparer.Ordinal);

        public NativeShaderVariants(string json)
        {
            var data = JSON.Parse(json);
            variants = data[VariantsKey].Children
                .Select(value => value.Children.Select(keyword => keyword.Value).ToArray())
                .OrderByDescending(value => value.Length).ToArray();
            runtimeKeywords = new HashSet<string>(
                data[RuntimeKeywordsKey].Children.Select(value => value.Value),
                StringComparer.Ordinal);
        }

        internal string[][] GetVariants() => variants;

        public string[] Resolve(IReadOnlyList<string> requestedKeywords)
        {
            var request = new HashSet<string>(requestedKeywords, StringComparer.Ordinal);
            var key = string.Join("|", request.OrderBy(value => value, StringComparer.Ordinal));
            if (resolvedRequests.TryGetValue(key, out var resolved))
                return resolved;

            // The player falls back to a retained subset. Compiling the requested combination in the editor
            // can invent effects absent from the game, such as fog on Change of scene's concrete braces.
            foreach (var variant in variants)
            {
                if (!variant.All(request.Contains))
                    continue;

                resolved = variant.Concat(request.Where(runtimeKeywords.Contains)).ToArray();
                resolvedRequests.Add(key, resolved);
                return resolved;
            }

            throw new InvalidOperationException("The native shader variant catalog must contain its empty baseline.");
        }
    }
}
