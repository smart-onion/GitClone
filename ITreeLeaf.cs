namespace GitClone
{
    public abstract class ITreeLeaf
    {
        public string Mode { get; set; }
        public string Path { get; set; }
        public string Sha { get; set; }
        public ITreeLeaf(string path, string mode, string sha)
        {
            Mode = mode;
            Path = path;
            Sha = sha;
        }
    }
}
