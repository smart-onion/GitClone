using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitClone
{
    public class GitLog
    {
        static HashSet<string> seen = new HashSet<string>();
        public static async Task CommitLog(GitRepository repo, string sha)
        {
            if (seen.Contains(sha) || sha == null) return;
            seen.Add(sha);

            var commitData = await GitObject.ReadObjectAsync(repo, sha);
            var commit = new GitCommit(commitData.Data);
            var shortHash = sha.Substring(0, 8);
            var message = commit.Message;
            if (message.Contains("\n"))
            {
                message = message.Substring(0, message.IndexOf("\n"));
            }

            Console.WriteLine($"  c_{shortHash} [label=\"{shortHash}: {message}\"];");
            if (commit.Parent == null || commit.Parent == "") return;

            Console.WriteLine($"  c_{shortHash} -> c_{commit.Parent?.Substring(0, 8) ?? ""};");
            await CommitLog(repo, commit.Parent);
        }

        public static void BranchLog()
        {
            var repo = GitRepository.GetInstance();
            var branches = Directory.GetFiles(repo.Heads);
            Console.WriteLine("Branches:");
            foreach ( var branch in branches)
            {
                Console.WriteLine($"\t-{Path.GetFileName(branch)}");
            } 
        }
    }
}
