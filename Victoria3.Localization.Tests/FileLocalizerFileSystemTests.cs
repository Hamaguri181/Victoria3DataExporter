using FluentAssertions;

namespace Victoria3.Localization.Tests
{
    public class FileLocalizerFileSystemTests
    {
        [Fact(DisplayName = "FromPath は実ファイルを読み込んでローカライズできる")]
        public void FromPath_ReadsSingleFile()
        {
            var dir = CreateTempDirectory();
            try
            {
                var file = Path.Combine(dir, "single.yml");
                File.WriteAllText(file, """
                    key:0 "value"
                    """);

                var localizer = FileLocalizer.FromPath(file);

                localizer.Localize("key").Should().Be("value");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        [Fact(DisplayName = "FromPaths は後勝ちでマージされる")]
        public void FromPaths_MergesWithLastWriteWins()
        {
            var dir = CreateTempDirectory();
            try
            {
                var file1 = Path.Combine(dir, "a.yml");
                var file2 = Path.Combine(dir, "b.yml");

                File.WriteAllText(file1, """
                    dup:0 "first"
                    """);
                File.WriteAllText(file2, """
                    dup:0 "second"
                    """);

                var localizer = FileLocalizer.FromPaths(new[] { file1, file2 });

                localizer.Localize("dup").Should().Be("second");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        [Fact(DisplayName = "FromDirectory は *.yml のみ再帰的に読み込む")]
        public void FromDirectory_ReadsOnlyYmlRecursively()
        {
            var dir = CreateTempDirectory();
            try
            {
                var sub = Path.Combine(dir, "sub");
                Directory.CreateDirectory(sub);

                File.WriteAllText(Path.Combine(dir, "root.yml"), """
                    root:0 "root-value"
                    """);
                File.WriteAllText(Path.Combine(sub, "child.yml"), """
                    child:0 "child-value"
                    """);
                File.WriteAllText(Path.Combine(dir, "ignore.txt"), """
                    ignored:0 "ignored-value"
                    """);

                var localizer = FileLocalizer.FromDirectory(dir);

                localizer.Localize("root").Should().Be("root-value");
                localizer.Localize("child").Should().Be("child-value");
                localizer.Localize("ignored").Should().Be("ignored");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        [Fact(DisplayName = "FromDirectory の並び順に基づき後勝ちで上書きされる")]
        public void FromDirectory_MergeOrder_IsApplied()
        {
            var dir = CreateTempDirectory();
            try
            {
                // 実装は OrderByDescending(f => f) なので z -> a の順に読み込まれ、a が最終的に勝つ
                File.WriteAllText(Path.Combine(dir, "a.yml"), """
                    dup:0 "A"
                    """);
                File.WriteAllText(Path.Combine(dir, "z.yml"), """
                    dup:0 "Z"
                    """);

                var localizer = FileLocalizer.FromDirectory(dir);

                localizer.Localize("dup").Should().Be("A");
            }
            finally
            {
                SafeDeleteDirectory(dir);
            }
        }

        private static string CreateTempDirectory()
        {
            var dir = Path.Combine(Path.GetTempPath(), "Victoria3.Localization.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static void SafeDeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
}