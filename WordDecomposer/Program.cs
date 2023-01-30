using System.Diagnostics;
using System.Text;

string[] simpleWords;
string[] toDecompose;
string outputPath = "";
InitFiles(out simpleWords, out toDecompose, out outputPath);

bool decomposed = false;
string first = "";
string second = "";
string third = "";
StringBuilder output = new StringBuilder();

Stopwatch sw = Stopwatch.StartNew();

//decomposition loop async parallel
foreach (string word in toDecompose)
{
    first = await SearchWordAsync(word.Trim(), false);
    if (word.Length > first.Length && first.Length > 0)
    {
        second = await SearchWordAsync(word.Substring(first.Length), true);
        decomposed = second.Length < 1 ? false : true;
    }
    if (word.Length > first.Length + second.Length && decomposed)
    {
        third = await SearchWordAsync(word.Substring(first.Length + second.Length), true);
    }
    if (decomposed)
    {
        output.Append($"{word} --- {first}, {second}, {third} \n");
        decomposed = false;
    }
    second = "";
    first = "";
    third = "";
}
sw.Stop();
Console.WriteLine($"Task done in parallel in {sw.Elapsed.TotalSeconds} seconds");

output.Clear();
sw.Restart();
//synchronous decomposition
foreach (string word in toDecompose)
{
    first = searchWord(word.Trim(), false);
    if (word.Length > first.Length && first.Length > 0)
    {
        second = searchWord(word.Substring(first.Length), true);
        decomposed = second.Length < 1 ? false : true;
    }
    if (word.Length > first.Length + second.Length && decomposed)
    {
        third = searchWord(word.Substring(first.Length + second.Length), true);
    }

    if (decomposed)
    {
        output.Append($"{word} --- {first}, {second}, {third} \n");
        decomposed = false;
    }
    second = "";
    first = "";
    third = "";
}
sw.Stop ();
Console.WriteLine($"Task done in {sw.Elapsed.TotalSeconds} seconds");

//writing decomposed words to file
using (StreamWriter outputWriter = new StreamWriter(outputPath))
{
    foreach (ReadOnlyMemory<char> c in output.GetChunks())
    {
        outputWriter.Write(c);
    }
}

//synchronous sesarch method
string searchWord(string word, bool findLongest)
{
    string longestWord = "";
    foreach (string w in simpleWords)
    {
        if (word.StartsWith(w.ToLower()))
        {
            if (longestWord.Length <= w.Length && w.Length <= word.Length && findLongest)
            {
                longestWord = w;
            }
            else if (longestWord.Length <= w.Length && w.Length < word.Length)
            {
                longestWord = w;
            }
        }
    }
    return longestWord;
}

//seearching for longest word async in Parallel
//TRUE to return exact word, even if it can be decomposed
async Task<string> SearchWordAsync(string word, bool findLongest)
{
    string longestWord = "";
    await Task.Run(() =>
    Parallel.ForEach(simpleWords, compare =>
    {
        if (word.StartsWith(compare.ToLower()))
        {
            if (longestWord.Length <= compare.Length && compare.Length <= word.Length && findLongest)
            {
                longestWord = compare;
            }
            else if (longestWord.Length <= compare.Length && compare.Length < word.Length)
            {
                longestWord = compare;
            }
        }
    }));

    return longestWord;
}

//initialization of Working Paths and loading words to memory
static void InitFiles(out string[] dictionary, out string[] toTest, out string output)
{
    string dictionaryPath = "";
    string testWordPath = "";

    string dictWords;
    string testWords;

    while (!File.Exists(dictionaryPath))
    {
        Console.WriteLine("Provide dictionary path");
        dictionaryPath = Console.ReadLine();
    }
    while (!File.Exists(testWordPath))
    {
        Console.WriteLine("Provide test words path");
        testWordPath = Console.ReadLine();
    }
    Console.WriteLine("Provide output file name (will be save in project directory DATA folder)");
    output = $"Data/{ Console.ReadLine()}.tsv";


    using (StreamReader dictReader = new StreamReader(dictionaryPath))
    {
        dictWords = dictReader.ReadToEnd();
    }

    using (StreamReader testReader = new StreamReader(testWordPath))
    {
        testWords = testReader.ReadToEnd();
    }

    dictionary = dictWords.Split("\n");
    toTest = testWords.Split("\r\n");
}
