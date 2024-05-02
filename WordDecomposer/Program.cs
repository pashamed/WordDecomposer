using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        ConcurrentDictionary<string, bool> dictionary;
        string[] toDecompose;
        string outputPath;
        InitFiles(out dictionary, out toDecompose, out outputPath);

        StringBuilder output = new StringBuilder();
        Stopwatch sw = Stopwatch.StartNew();

        ConcurrentBag<string> results = new ConcurrentBag<string>();

        await Task.WhenAll(toDecompose.AsParallel().Select(word => ProcessWordAsync(word, dictionary, results)));

        foreach (var result in results)
        {
            output.AppendLine(result);
        }

        sw.Stop();
        Console.WriteLine($"Task done in parallel in {sw.Elapsed.TotalSeconds} seconds");

        await File.WriteAllTextAsync(outputPath, output.ToString());
    }

    private static async Task ProcessWordAsync(string word, ConcurrentDictionary<string, bool> dictionary, ConcurrentBag<string> results)
    {
        var decompositions = await DecomposeWordAsync(word, dictionary, new List<string>());
        if (decompositions.Any())
        {
            var result = $"{word} --- {string.Join(", ", decompositions.First())}";
            results.Add(result);
        }
    }

    private static async Task<List<List<string>>> DecomposeWordAsync(string remainingWord, ConcurrentDictionary<string, bool> dictionary, List<string> currentDecomposition)
    {
        var decompositions = new List<List<string>>();
        if (string.IsNullOrEmpty(remainingWord))
        {
            decompositions.Add(new List<string>(currentDecomposition));
            return decompositions;
        }

        bool foundDecomposition = false;

        for (int i = remainingWord.Length; i > 0; i--)
        {
            var subWord = remainingWord.Substring(0, i);
            if (subWord.Length >= 3 && dictionary.ContainsKey(subWord))
            {
                var newDecomposition = new List<string>(currentDecomposition) { subWord };
                var subDecompositions = await DecomposeWordAsync(remainingWord.Substring(i), dictionary, newDecomposition);
                if (subDecompositions.Any())
                {
                    decompositions.AddRange(subDecompositions);
                    foundDecomposition = true;
                }
            }
        }

        if (!foundDecomposition && currentDecomposition.Count > 0)
        {
            decompositions.Add(new List<string>(currentDecomposition) { remainingWord });
        }

        return decompositions;
    }

    private static void InitFiles(out ConcurrentDictionary<string, bool> dictionary, out string[] toTest, out string output)
    {
        string dictionaryPath = "Data/de-dictionary.tsv";
        string testWordPath = "Data/de-test-words.tsv";
        output = "Data/output.tsv";

        string dictWords = File.ReadAllText(dictionaryPath).ToLowerInvariant();
        string testWords = File.ReadAllText(testWordPath).ToLowerInvariant();

        var dictEntries = dictWords.Split("\n", StringSplitOptions.RemoveEmptyEntries).Distinct()
                                   .ToDictionary(word => word.Trim(), word => true);
        dictionary = new ConcurrentDictionary<string, bool>(dictEntries);
        toTest = testWords.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
    }
}