using System.Text.Json;

namespace GitClone
{
    public class GitRepository
    {
        public string WorkTree { get; private set; }
        public string GitDir { get; private set; }
        public string Refs { get; private set; }
        public string Objsects { get; private set; }
        public string Heads { get; private set; }
        public GitConfig GitConfig { get; private set; }

        private static GitRepository instance;
        private GitRepository(string path)
        {
            WorkTree = path;
            GitDir = Path.Combine(path, ".git");
            Refs = Path.Combine(GitDir, "refs");
            Objsects = Path.Combine(GitDir, "objects");
            Heads = Path.Combine(Refs, "heads");
            
        }

        public static GitRepository InitInstance(string path)
        {
            if (instance == null)
            {
                instance = new GitRepository(path);
                return instance;
            }
            else return GetInstance();
        }

        public static GitRepository GetInstance()
        {
            return instance;
        }

        public void CreateRepository()
        {
            Directory.CreateDirectory(GitDir);
            Directory.CreateDirectory(Path.Combine(GitDir, "objects"));
            Directory.CreateDirectory(Path.Combine(GitDir, "refs", "tags"));
            Directory.CreateDirectory(Path.Combine(GitDir, "refs", "heads"));
            Directory.CreateDirectory(Path.Combine(GitDir, "branches"));
            File.WriteAllText(Path.Combine(GitDir, "HEAD"), JsonSerializer.Serialize(new Reference(JsonSerializer.Serialize(new Reference(Path.Combine("refs", "heads", "main")))))); //"ref: refs/heads/main\n"
            Console.WriteLine("Initialized git directory");
        }

        public static async Task<GitObjectFile?> FindObjectFileAsync(string sha)
        {
            return await GitObject.ReadObjectAsync(instance, sha);
        }
    }
}
