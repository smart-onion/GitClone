using System.Text.Json.Serialization;

namespace GitClone
{
    public struct GitConfig
    {
        public int RepositoryFormatVersion;
        public bool FileMode;
        public bool Bare;

        public GitConfig()
        {
            RepositoryFormatVersion = 0;
            FileMode = false;
            Bare = false;
        }

        [JsonConstructor]
        public GitConfig(int repositoryFormatVersion, bool fileMode, bool bare)
        {
            RepositoryFormatVersion = repositoryFormatVersion;
            FileMode = fileMode;
            Bare = bare;
        }
    }
}
