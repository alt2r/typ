using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace third_year_project.Services
{
    internal class IntArrayParser
    {
        public static int[][] ParseTo2DIntArray(string treeStructure)
        {
            if (string.IsNullOrWhiteSpace(treeStructure))
                return Array.Empty<int[]>();

            // Find all occurrences of stuff in square brackets and then send it all out as an array
            var matches = Regex.Matches(treeStructure, @"\[(.*?)\]");
            if (matches.Count == 0)
                return Array.Empty<int[]>();

            var result = new int[matches.Count][];
            for (int i = 0; i < matches.Count; i++)
            {
                var content = matches[i].Groups[1].Value.Trim();
                if (string.IsNullOrEmpty(content))
                {
                    result[i] = Array.Empty<int>();
                    continue;
                }

                var parts = content.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();

                var ints = new int[parts.Length];
                for (int j = 0; j < parts.Length; j++)
                {
                    if (!int.TryParse(parts[j], out ints[j]))
                        throw new FormatException($"Invalid integer token '{parts[j]}' in TreeStructure.");
                }
                result[i] = ints;
            }
            return result;
        }
    }
}
