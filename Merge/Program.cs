
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var dir = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
        DirectoryInfo d = new DirectoryInfo(dir);

        FileInfo[] Files = d.GetFiles("*.cs", SearchOption.AllDirectories);

        StringBuilder sb = new StringBuilder();

        var usingList = new List<string>();
        foreach (FileInfo file in Files)
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


