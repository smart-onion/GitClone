using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitClone
{
    public class GitIndex
    {
        public List<GitIndexEntry> entries;
        public GitIndex()
        {
            if (entries == null) entries = new List<GitIndexEntry>();
        }

        public void AddNode(GitIndexEntry entry)
        {
            entries.Add(entry);
        }

        public void RemoveEntry(string path)
        {
            entries.RemoveAll(e => e.Path == path);
        }

        public GitIndexEntry FindEntry(string path)
        {
            return entries.Find(e => e.Path == path);
        }

        public void WriteIndex(string? indexPath = null)
        {
            if (indexPath == null) indexPath = GitRepository.GetInstance().GitDir;
            var json = JsonSerializer.Serialize(entries);
            File.WriteAllText(Path.Combine(indexPath, "index"), json);
        }

        public void ReadIndex(string? indexPath = null)
        {
            if (indexPath == null) indexPath = Path.Combine(GitRepository.GetInstance().GitDir, "index");

            if (File.Exists(indexPath))
            {
                var json = File.ReadAllText(indexPath);
                entries = JsonSerializer.Deserialize<List<GitIndexEntry>>(json);
            }
        }
        public static async Task<bool> IsChangedAsync()
        {
            var repo = GitRepository.GetInstance();
            var index = new GitIndex();
            index.ReadIndex();

            var modif = index.entries.Where(e => e.Modified == true).ToList();
            if (modif.Any()) return true;
            return false;
        }
        public override string ToString()
        {
            return string.Join(Environment.NewLine, entries);
        }
    }
}
