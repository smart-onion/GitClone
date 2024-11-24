using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitClone
{
    public class GitTree : GitObject
    {
        public GitTreeLeaf Root { get; set; }
        public GitTree(string? path = "/", byte[]? data = null): base("tree") 
        {
            if (data == null)
            {
                Root = new GitTreeLeaf(path, "040000",  null);
            }
            else
            {
                Deserialize(data);
            }
        }

        [JsonConstructor]
        public GitTree(GitTreeLeaf root) :base("tree")
        {
            Root = root;
        }

        public void AddNode(string path, string mode,  string sha)
        {
            var parts = path.Split('/');
            var current = Root;

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                var child = current.Children.Find(n => n.Path == part);

                if (child == null)
                {
                    child = new GitTreeLeaf(part, i == parts.Length - 1 ? mode : "040000",  i == parts.Length - 1 ? sha : null);
                    current.Children.Add(child);
                }

                current = child;
            }
        }

        public override void Deserialize(byte[] jsonString)
        {
            var tree = JsonSerializer.Deserialize<GitTree>(jsonString);
            this.Root = tree.Root;
        }

        public override void Init()
        {
            throw new NotImplementedException();
        }

        public override byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
        }

        public void LsTree(string path = "/", int depth = -1)
        {
            var node = FindNode(path);
            if (node == null)
            {
                Console.WriteLine("Path not found.");
                return;
            }

            LsTreeRecursive(node, depth, 0);
        }

        private void LsTreeRecursive(GitTreeLeaf node, int maxDepth, int currentDepth)
        {
            if (maxDepth != -1 && currentDepth > maxDepth)
            {
                return;
            }

            foreach (var child in node.Children)
            {
                Console.WriteLine($"{child.Mode} {child.Sha} {child.Path}");
                if (child.Mode == "040000") // Directory
                {
                    LsTreeRecursive(child, maxDepth, currentDepth + 1);
                }
            }
        }

        private GitTreeLeaf FindNode(string path)
        {
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var current = Root;

            foreach (var part in parts)
            {
                current = current.Children.Find(n => n.Path == part);
                if (current == null)
                {
                    return null;
                }
            }

            return current;
        }
    }
}
