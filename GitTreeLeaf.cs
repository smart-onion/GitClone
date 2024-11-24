using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GitClone
{
    public class GitTreeLeaf: ITreeLeaf
    {
        public List<GitTreeLeaf> Children { get; set; } 
        public GitTreeLeaf(string path, string mode, string sha) :base(path, mode, sha) 
        {
            Children = new List<GitTreeLeaf>();
        }
        [JsonConstructor]
        public GitTreeLeaf(string path, string mode, string sha, List<GitTreeLeaf> children) : base(path, mode, sha)
        {
            Children = children;
        }
    }
}
