using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GitClone
{
    public abstract class GitObject
    {
        public byte[] Data { get; set; }
        protected string name;

        public GitObject(string name, byte[]? data = null)
        {
            this.name = name;
            if (data != null)
            {
                Deserialize(data);
            }
        }

        public abstract byte[] Serialize();
        public abstract void Deserialize(byte[] jsonString);
        public abstract void Init();

        public static async Task<GitObjectFile> ReadObjectAsync(GitRepository repo, string sha)
        {
            var path = Path.Combine(repo.GitDir, "objects", sha[..2], sha[2..]);
            if (!File.Exists(path)) throw new Exception("Path does not exist");

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var zLib = new ZLibStream(fileStream, CompressionMode.Decompress))
            using (var ms = new MemoryStream())
            {
                await zLib.CopyToAsync(ms);
                var content = Encoding.UTF8.GetString(ms.ToArray());//.Replace("\0", string.Empty);
                var objFile = JsonSerializer.Deserialize<GitObjectFile>(content);
                return objFile;
            }
        }

        public static async Task<string> WriteObjectAsync(GitObject obj, GitRepository repo)
        {
            var data = obj.Serialize();
            var objFile = new GitObjectFile(obj.name, data.Length, data);
            var jsonString = JsonSerializer.Serialize(objFile);
            var sha = Utility.ComputeSha1Hash(jsonString);
            var dirPath = Path.Combine(repo.GitDir, "objects", sha[..2]);
            Directory.CreateDirectory(dirPath);

            using (var writer = new FileStream(Path.Combine(dirPath, sha[2..]), FileMode.Create, FileAccess.Write))
            using (var zLibStream = new ZLibStream(writer, CompressionLevel.Optimal))
            {
                await zLibStream.WriteAsync(Encoding.UTF8.GetBytes(jsonString), 0, jsonString.Length);
            }

            return sha;
        }
    }
}