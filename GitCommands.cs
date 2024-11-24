using System;
using System.Reflection;
using System.Text.Json;

namespace GitClone
{
    public static partial class GitCommands
    {

        public static async Task Checkout(string commitSha, string path = ".")
        {
            var commitObject = await GitRepository.FindObjectFileAsync(commitSha);

            if (commitObject.Name != "commit") throw new Exception($"Object {commitSha} is not a commit object!");

            GitCommit commit = new GitCommit(commitObject.Data);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            else if (Directory.GetFiles(path).Length > 0 || Directory.GetDirectories(path).Length > 0)
            {
                foreach(var p in Directory.EnumerateFileSystemEntries(path))
                {
                    if (File.Exists(p)) File.Delete(p);
                    if (Directory.Exists(p)) Directory.Delete(p);
                }

                //throw new Exception($"Directory {path} is not empty");
            }

            var objctTree = await GitRepository.FindObjectFileAsync(commit.Tree);
            if (objctTree.Name != "tree") throw new Exception($"Object {commit.Tree} is not a tree object!");

            GitTree tree = new GitTree("/",objctTree.Data);
            await CheckoutTree(tree.Root, path);

        }

        public static async Task SwitchOrCreateBranch(string name, string flag)
        {
            var head = await GetHead();
            var repo = GitRepository.GetInstance();
            var pathRef = Path.Combine("refs", "heads", name);


            if (!Path.Exists(Path.Combine(repo.GitDir, pathRef)) && flag == "-n")
            {
                Console.WriteLine($"Branch '{name}' created");
            }
            else if (!Path.Exists(Path.Combine(repo.GitDir, pathRef)))
            {
                Console.WriteLine($"Branch '{name}' not exist");
                return;
            }
            else if (Path.Exists(Path.Combine(repo.GitDir, pathRef))) Console.WriteLine($"Switch to branch '{name}'");

            head.Ref = JsonSerializer.Serialize(new Reference(pathRef));
            File.WriteAllText(Path.Combine(repo.GitDir, "HEAD"), JsonSerializer.Serialize(head));
        }

        public static async Task<Dictionary<string, string>> ListRefsAsync(GitRepository repo, string path = null)
        {
            if (path == null) path = Path.Combine(repo.GitDir, "refs");

            var refs = new Dictionary<string, string>();
            foreach (var dir in Directory.GetDirectories(path))
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    string refName = Path.GetFileName(file);
                    refs[refName] = await ResolveRefAsync(refName);
                }
            }
            return refs;
        }

        public static void CreateTag(GitRepository repo, string name, string sha)
        {
            string tagPath = Path.Combine(repo.GitDir, "refs", "tags", name);
            string tagDir = Path.GetDirectoryName(tagPath);
            if (!Directory.Exists(tagDir))
            {
                Directory.CreateDirectory(tagDir);
            }
            File.WriteAllText(tagPath, JsonSerializer.Serialize(new Reference(sha)));
        }

        public static Dictionary<string, string> ListTags(GitRepository repo)
        {
            string tagsPath = Path.Combine(repo.GitDir, "refs", "tags");
            var tags = new Dictionary<string, string>();

            foreach (var file in Directory.GetFiles(tagsPath))
            {
                string tagName = Path.GetFileName(file);
                string tagSha = File.ReadAllText(file).Trim();
                tags[tagName] = tagSha;
            }

            return tags;
        }

        public static async Task<string> ObjectFind(string name, string fmt = null, bool follow = true)
        {
            var shaList = await ObjectResolveAsync(name);

            if (shaList == null || shaList.Count == 0)
            {
                throw new Exception($"No such reference {name}.");
            }

            if (shaList.Count > 1)
            {
                throw new Exception($"Ambiguous reference {name}: Candidates are:\n - {string.Join("\n - ", shaList)}");
            }

            string resolvedSha = shaList[0];

            if (fmt == null)
            {
                return resolvedSha;
            }

            var objFile = await GitObject.ReadObjectAsync(GitRepository.GetInstance(), resolvedSha);

            if (objFile.Name == fmt)
            {
                return resolvedSha;
            }

            if (!follow)
            {
                return null;
            }

            if (objFile.Name == "commit" && fmt == "tree")
            {
                var commit = JsonSerializer.Deserialize<GitCommit>(objFile.Data);
                if (commit != null)
                {
                    return commit.Tree;
                }
            }

            return null;
        }

        public static async Task Add()
        {
            var index = await ReadFilesToIndex();

            var oldIndex = new GitIndex();
            oldIndex.ReadIndex();
            foreach (var file in index.entries)
            {
                var entry = oldIndex.entries.FirstOrDefault(e => e.Sha == file.Sha);
                if (entry == null)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Added: {file.Path}");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            index.WriteIndex();
        }
        public static async Task Add(string entry)
        {
            if (entry == "*")
            {
                await Add();
                return;
            }
            
            GitIndexEntry newEntry;
            var existedIndex = new GitIndex();
            existedIndex.ReadIndex();
            if (File.Exists(entry))
            {
                var blob = new GitBlob(entry);
                var sha = await GitObject.WriteObjectAsync(blob, GitRepository.GetInstance());

                newEntry = new GitIndexEntry(entry, "100644", sha);
                var exitedEntry = existedIndex.entries.FirstOrDefault(x => x.Path == entry);

                if (exitedEntry == null || exitedEntry.Sha != sha) newEntry.Modified = true;

                existedIndex.AddNode(newEntry);
            }
            else if (Directory.Exists(entry))
            {
                var tree = new GitTree(entry);
                await AddTreeAsync(entry, tree);
                var treeSha = await GitObject.WriteObjectAsync(tree, GitRepository.GetInstance());
                newEntry = new GitIndexEntry(entry, "040000", treeSha);
                var exitedEntry = existedIndex.entries.FirstOrDefault(x => x.Path == entry);

                if (exitedEntry == null || exitedEntry.Sha != treeSha) newEntry.Modified = true;

                existedIndex.AddNode(newEntry);
            }

            existedIndex.WriteIndex();

        }

        public static void Remove(string fileToRemove)
        {
            var index = new GitIndex();
            index.ReadIndex();

            if (fileToRemove == "*") index.entries.Clear();
            else index.entries.RemoveAll(x => Path.Equals(x.Path, fileToRemove));
            index.WriteIndex();
        }

        public static async Task Status()
        {
            var repo = GitRepository.GetInstance();
            var index = new GitIndex();
            index.ReadIndex();
            var currentIndex = await ReadFilesToIndex(".");

            var modified = index.entries.ExceptBy(currentIndex.entries.Select(c => c.Sha), i => i.Sha).ToList();
            var newfiels = IsEntryRemovedOrAdded(index.entries, currentIndex.entries);
            
            Console.ForegroundColor = ConsoleColor.White;
            var branch = await GetCurrentBranch();
            Console.WriteLine($"Current branch: '{branch}'");
            
            if (newfiels.Count == 0 && modified.Count == 0) Console.WriteLine("Nothing to add");
            
            else Console.WriteLine("Not added files: ");
            
            Console.ForegroundColor = ConsoleColor.Red;
            modified.ForEach(a => {
                if (Path.Exists(a.Path)) Console.WriteLine($"\tmodified: {a.Path}");
            });

            newfiels.ForEach(a => Console.WriteLine(a));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.ForegroundColor = ConsoleColor.White;
            
            if (index.entries.Any(a => a.Modified == true)) Console.WriteLine("Files to commit:");

            Console.ForegroundColor = ConsoleColor.Green;

            foreach (var file in index.entries)
            {
                if (file.Modified)
                {
                    Console.WriteLine($"\tmodified: {file}");

                }
            }
            Console.ForegroundColor = ConsoleColor.White;

        }

        public static async Task Commit(string message)
        {
            if (!await GitIndex.IsChangedAsync())
            {
                Console.WriteLine("Nothing to commit...");
                return;
            }
            var repo = GitRepository.GetInstance();
            var head = await GetHead();
            var path = Path.Combine(".git", head.Ref);
            var separator = Path.DirectorySeparatorChar;
            path = path.Replace('/',separator);
            var branch = Path.GetFileName(path);
            GitCommit commit = new GitCommit();
            if (!Path.Exists(Path.Combine(path)))
            {
                using (StreamWriter writer = new(path, new FileStreamOptions { Mode = FileMode.Create , Access = FileAccess.ReadWrite})) 
                { 
                    writer.Close(); 
                }
                commit.Parent = null;
            }
            else
            {
                using (StreamReader reader = new(path))
                {
                    var shaR = reader.ReadToEnd();
                    commit.Parent = shaR;
                }
            }

            var index = new GitIndex();
            index.ReadIndex();

            var tree = new GitTree();

            foreach (var entry in index.entries)
            {
                tree.AddNode(entry.Path, entry.Mode, entry.Sha);
            }

            var sha = await GitObject.WriteObjectAsync(tree, GitRepository.GetInstance());

            commit.Tree = sha;
            commit.Message = message;

            var commitSha = await GitObject.WriteObjectAsync(commit, GitRepository.GetInstance());

            using (StreamWriter writer = new(path))
            {
                await writer.WriteAsync(commitSha);
            }

            Console.WriteLine($"Commit: {commitSha[..8]}");
            index.entries.ForEach(entry => entry.Modified = false);
            index.WriteIndex();
        }
       
    }
}
