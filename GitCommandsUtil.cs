using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GitClone
{
    public static partial class GitCommands
    {
        private static async Task CheckoutTree(GitTreeLeaf node, string path)
        {
            foreach (var child in node.Children)
            {
                var dest = System.IO.Path.Combine(path, child.Path);
                if (child.Mode == "040000") // Directory
                {
                    System.IO.Directory.CreateDirectory(dest);
                    await CheckoutTree(child, dest);
                }
                else if (child.Mode == "100644") // File
                {
                    var objBlob = await GitRepository.FindObjectFileAsync(child.Sha);
                    if (objBlob.Name != "blob") throw new Exception($"Object {child.Sha} is not a blob object!");

                    GitBlob blob = new GitBlob();
                    blob.Data = objBlob.Data;
                    System.IO.File.WriteAllBytes(dest, blob.Serialize()); // Placeholder for actual blob content
                }
            }
        }
        public static async Task<string> CommitResolveAsync(string name,string sha = "HEAD")
        {
            if (sha == "HEAD") 
            {
                sha = await GitCommands.ObjectFind("HEAD");
            }
            var commitObj = await GitObject.ReadObjectAsync(GitRepository.GetInstance(), sha);
            if (commitObj == null) throw new Exception("Commit not exist!");

            var commit = JsonSerializer.Deserialize<GitCommit>(commitObj.Data);

            if (commit == null) throw new Exception("Bad commit data");
            
            if (commit.Message == name)
            {
                return sha;
            }
            else if (commit.Parent == null)
            {
                throw new Exception("Commit not exist!");
            }
            else return await CommitResolveAsync(name, commit.Parent);
        }
        private static async Task<string> ResolveRefAsync(string refName)
        {
            string path;

            var files = Directory.GetFiles(".", "*", SearchOption.AllDirectories);

            path = files.FirstOrDefault(f => Path.GetFileName(f) == Path.GetFileName(refName)) ?? string.Empty;


            if (!Path.Exists(path)) return null;

            string data;

            using (StreamReader reader = new StreamReader(path))
            {
                data = await reader.ReadToEndAsync();
            }

            data = data.Replace("\0", string.Empty);
            return await RefRecursionAsync(data);
        }

        private static async Task<string> RefRecursionAsync(string data)
        {
            if (data.StartsWith("{"))
            {
                data = data.Replace("\0", string.Empty);
                var refer2 = JsonSerializer.Deserialize<Reference>(data);
                return await RefRecursionAsync(refer2.Ref);
            }
            else 
            {
                var path = Path.Combine(GitRepository.GetInstance().GitDir, data);
                if (!Path.Exists(path)) throw new Exception("Path not exist!");
                using (StreamReader reader = new StreamReader(path))
                {
                    return await reader.ReadToEndAsync();
                }
            }
        }

        private static async Task<List<string>> ObjectResolveAsync(string name)
        {
            var candidates = new List<string>();
            var hashRE = new Regex("^[0-9A-Fa-f]{4,40}$");

            //if (isTag)
            //{
            //    var path = Path.Combine(GitRepository.GetInstance().GitDir, "refs", "tags", name);
            //    if (File.Exists(path))
            //    {
            //        candidates.Add(await File.ReadAllTextAsync(path));
            //    }
            //    return candidates;
            //}

            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            if (name == "HEAD")
            {
                var headRef = await ResolveRefAsync("HEAD");
                if (headRef != null)
                {
                    candidates.Add(headRef);
                }
                return candidates;
            }

            if (hashRE.IsMatch(name))
            {
                name = name.ToLower();
                string prefix = name.Substring(0, 2);
                string path = Path.Combine(GitRepository.GetInstance().GitDir, "objects", prefix);

                if (Directory.Exists(path))
                {
                    string rem = name.Substring(2);
                    foreach (var file in Directory.GetFiles(path))
                    {
                        if (Path.GetFileName(file).StartsWith(rem))
                        {
                            candidates.Add(prefix + Path.GetFileName(file));
                        }
                    }
                }
            }

            string asTag = await ResolveRefAsync(Path.Combine("tags", name));
            if (asTag != null)
            {
                candidates.Add(asTag);
            }

            string asBranch = await ResolveRefAsync($"refs/heads/{name}");
            if (asBranch != null)
            {
                candidates.Add(asBranch);
            }

            return candidates;
        }

        private static async Task<string> AddTreeAsync(string currentPath, GitTree tree)
        {
            var treeSha = string.Empty;

            foreach (var entry in Directory.EnumerateFileSystemEntries(currentPath))
            {
                if (Directory.Exists(entry))
                {
                    var treeNew = new GitTree(entry);
                    treeSha = await GitObject.WriteObjectAsync(treeNew, GitRepository.GetInstance());
                    tree.AddNode(entry, "040000",  treeSha);
                    await AddTreeAsync(currentPath, treeNew);
                }
                else if (File.Exists(entry))
                {
                    var blob = new GitBlob(entry);
                    var sha = await GitObject.WriteObjectAsync(blob, GitRepository.GetInstance());
                    tree.AddNode(entry, "100644",  sha);
                }
            }
            return treeSha;
        }

        public static async Task<GitIndex> ReadFilesToIndex(string? directoryPath = null)
        {
            var repo = GitRepository.GetInstance();
            var path = directoryPath ?? repo.WorkTree;
            var index = new GitIndex();
            GitIndexEntry newEntry;
            var existedIndex = new GitIndex();
            existedIndex.ReadIndex();
   
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                if (entry == repo.GitDir) continue;
                if (Directory.Exists(entry))
                {
                    var tree = new GitTree(entry);
                    await AddTreeAsync(entry, tree);
                    var treeSha = await GitObject.WriteObjectAsync(tree, GitRepository.GetInstance());
                    newEntry = new GitIndexEntry(entry, "040000", treeSha);
                    var exitedEntry = existedIndex.entries.FirstOrDefault(x => x.Path == entry);

                    if (exitedEntry == null || exitedEntry.Sha != treeSha) newEntry.Modified = true;

                    index.AddNode(newEntry);
                }
                else if (File.Exists(entry))
                {
                    var blob = new GitBlob(entry);
                    var sha = await GitObject.WriteObjectAsync(blob, GitRepository.GetInstance());

                    newEntry = new GitIndexEntry(entry, "100644", sha);
                    var exitedEntry = existedIndex.entries.FirstOrDefault(x => x.Path == entry);

                    if (exitedEntry == null || exitedEntry.Sha != sha) newEntry.Modified = true;

                    index.AddNode(newEntry);
                }
            }
            
            return index;
        }

        public static List<string> IsEntryRemovedOrAdded(List<GitIndexEntry> list1, List<GitIndexEntry> list2)
        {
            var set1 = list1.Select(l => Path.GetFileName(l.Path)).ToList();
            var set2 = list2.Select(l => Path.GetFileName(l.Path)).ToList();
            List<string> result = new();
            var removed = set1.Except(set2).ToList(); //removed
            var added = set2.Except(set1).ToList(); //added

            if (removed.Count == 0 && added.Count == 0) return result;

            removed.ForEach(l => result.Add($"\tremoved : {l}"));
            added.ForEach(l => result.Add($"\tnew\t: {l}"));

            return result;
        }

        private static async Task<Reference?> GetHead()
        {
            string jsonString = string.Empty;
            using (StreamReader reader = new(Path.Combine(".git", "HEAD")))
            {
                jsonString = await reader.ReadToEndAsync();
            }
            var json = JsonSerializer.Deserialize<Reference>(jsonString);
            return JsonSerializer.Deserialize<Reference>(json.Ref);
        }

        private static async Task<string> GetCurrentBranch()
        {
            var head = await GetHead();
            return Path.GetFileName(head.Ref);
        }
    }
}
