using GitClone;
using Microsoft.VisualBasic;
using System.ComponentModel.Design;
using System.Reflection.Metadata;
using System.Text.Json;

internal class Program
{
    static GitRepository repo;

    private static async Task Main(string[] args)
    {

        repo = GitRepository.InitInstance(".");
        await RunCommand(args);

    }

    private static async Task RunCommand(string[] args)
    {
        var repo = GitRepository.GetInstance();
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a command!");
            return;
        }

        string cmd = args[0];

        switch (cmd)
        {
            case "init":
                repo.CreateRepository();
                break;
            case "add":
                if (args.Length < 2)
                {
                    Console.WriteLine("File not specefied!");
                    return;
                }
                await GitCommands.Add(args[1]);
                break;
            case "rm":
                GitCommands.Remove(args[1]);
                break;
            case "status":
                await GitCommands.Status();
                break;
            case "checkout":
                var shaC = await GitCommands.CommitResolveAsync(args[1]);
                var path = "";
                if (args.Length == 3)
                {
                    path = Path.Combine(Directory.GetCurrentDirectory(), args[2]);
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                }
                else path = ".";

                await GitCommands.Checkout(shaC, path);

                break;
            case "branch":
                var flag = "";
                var name = "";
                if (args.Length == 3)
                {
                    flag = args[1];
                    name = args[2];
                }
                else name = args[1];
                await GitCommands.SwitchOrCreateBranch(name, flag);
                break;
            case "ls-branch":
                GitLog.BranchLog();
                break;
            case "ls-commit":
                var sha = await GitCommands.ObjectFind("HEAD");

                await GitLog.CommitLog(repo, sha);
                break;
            case "commit":
                await GitCommands.Commit(args[1]);
                break;
            default:
                break;
        }
    }
}