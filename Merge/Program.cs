
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 0) return ;
        var dirs = new List<string>();
        var files = new List<FileInfo>();

        foreach (var dir in args)
        {
            DirectoryInfo d = new DirectoryInfo(dir);

            FileInfo[] Files = d.GetFiles("*.cs", SearchOption.AllDirectories);

            files.AddRange(Files);
        }
        

        StringBuilder sb = new StringBuilder();

        var usingList = new List<string>();
        foreach (FileInfo file in files)
        {
            var lines = File.ReadAllLines(file.FullName);
            if (lines.Length <= 0 || lines[0].Trim() != "// algo") continue;
            Console.WriteLine($"Managing {file.Name}");
            foreach (string line in lines)
            {
                if (line.Trim().StartsWith("using ")) {
                    if (!usingList.Contains(line.Trim()))  usingList.Add(line);
                } else     sb.AppendLine(line);
            }
        }

        StringBuilder usings = new StringBuilder();
    
        foreach (string line in usingList) usings.AppendLine(line);

        var result = usings.ToString() + Environment.NewLine + sb.ToString();
        Clipboard.SetText(result);
        Console.WriteLine("Merged Files");
    }
}


