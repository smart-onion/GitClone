using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitClone
{
    public class GitCommit : GitObject
    {

        public string Tree { get; set; }
        public string? Parent { get; set; }
        public string? Author { get; set; }
        public string? Commiter { get; set; }
        public string Message { get; set; }

        public GitCommit(byte[]? data = null) :base("commit",data) { }

        [JsonConstructor]
        public GitCommit(string tree, string? parent, string? author, string? commiter, string message) :base("commit")
        {
            this.Tree = tree;
            this.Parent = parent;   
            this.Author = author;
            this.Commiter = commiter;
            this.Message = message;
        }
        public override void Deserialize(byte[] jsonString)
        {
            var gitCommit = JsonSerializer.Deserialize<GitCommit>(jsonString);
            if (gitCommit == null) throw new Exception("GitCommit file error!");

            this.Tree = gitCommit.Tree;
            this.Parent = gitCommit.Parent;
            this.Author = gitCommit.Author;
            this.Commiter = gitCommit.Commiter;
            this.Message = gitCommit.Message;
        }
        public override void Init()
        {
            throw new NotImplementedException();
        }

        public override byte[] Serialize()
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
        }


    }
}
