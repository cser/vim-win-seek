using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class Seek
{
	public static void Main(string[] args)
	{
		string directory = null;
		string regexText = null;
		string filter = null;
		for (int i = 0; true;)
		{
			string arg = GetArg(args, i++);
			if (arg == null)
				break;
			if (arg == "-r")
			{
				regexText = GetArg(args, i++);
				if (regexText == null)
				{
					Console.Error.WriteLine("No regex after -r");
					LogHelp();
					return;
				}
			}
			else if (arg == "-d")
			{
				directory = GetArg(args, i++);
				if (directory == null)
				{
					Console.Error.WriteLine("No directory after -d");
					LogHelp();
					return;
				}
			}
			else if (arg == "-f")
			{
				filter = GetArg(args, i++);
				if (filter == null)
				{
					Console.Error.WriteLine("No filter after -f");
					LogHelp();
					return;
				}
			}
		}
		if (string.IsNullOrEmpty(regexText))
		{
			Console.Error.WriteLine("Parameter -r <regex> not specified");
			LogHelp();
			return;
		}
		Regex regex = null;
		string pattern = null;
		if (regexText.Length > 2 && regexText[0] == '/' && regexText.LastIndexOf("/") > 1)
		{
			RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.Multiline;
			int lastIndex = regexText.LastIndexOf("/");
			string optionsText = regexText.Substring(lastIndex + 1);
			string rawRegex = regexText.Substring(1, lastIndex - 1);
			for (int i = 0; i < optionsText.Length; i++)
			{
				char c = optionsText[i];
				if (c == 'i')
					options |= RegexOptions.IgnoreCase;
				else if (c == 's')
					options &= ~RegexOptions.Multiline;
				else if (c == 'e')
					options |= RegexOptions.ExplicitCapture;
				else
				{
					Console.Error.WriteLine("Unsupported regex option: " + c);
					LogHelp();
					return;
				}
			}
			try
			{
				regex = new Regex(rawRegex, options);
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Incorrect regex: " + regexText + " - " + e.Message);
				return;
			}
		}
		else
		{
			pattern = regexText;
		}
		Search(directory, regex, pattern, filter);
	}

	private static void LogHelp()
	{
		Console.WriteLine("Allowed parameters: -r <regex> [-d <directory> -f <filter>]\n" +
			"If pattern contains spaces, it mast be enclosed in \" \" pair:\n" +
			"\t\"some pattern\"\n" +
			"If pattern is regex, it mast be enclosed in / / pair:\n" +
			"\t/regex/\n" +
			"\t/another_regex/ie\n" +
			"Allowed regex options:\n" +
			"\ti - ignore case;\n" +
			"\ts - single line, ^ - is start of file, $ - end of file;\n" +
			"\te - explicit capture");
	}

	private static string GetArg(string[] args, int i)
	{
		if (i >= args.Length)
			return null;
		string arg = args[i];
		if (arg.Length > 2 && arg[0] == '"' && arg[arg.Length - 1] == '"')
			return arg.Substring(1, arg.Length - 2);
		return arg;
	}

	private static void Search(string directory, Regex regex, string pattern, string filter)
	{
		StringBuilder builder = new StringBuilder();
		bool needCutCurrent = false;
		if (string.IsNullOrEmpty(filter))
			filter = "*";
		if (string.IsNullOrEmpty(directory))
		{
			directory = Directory.GetCurrentDirectory();
			needCutCurrent = true;
		}
		string[] files = null;
		try
		{
			files = Directory.GetFiles(directory, filter, SearchOption.AllDirectories);
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("File list reading error: " + e.Message);
			return;
		}
		string currentDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar;
		List<int> indices = new List<int>();
		foreach (string file in files)
		{
			string text = File.ReadAllText(file);
			indices.Clear();
			if (regex != null)
			{
				MatchCollection matches = regex.Matches(text);
				if (matches.Count == 0)
					continue;
				foreach (Match match in matches)
				{
					indices.Add(match.Index);
				}
			}
			else
			{
				int index = text.IndexOf(pattern);
				if (index == -1)
					continue;
				while (true)
				{
					indices.Add(index);
					index = text.IndexOf(pattern, index + 1);
					if (index == -1)
						break;
				}
			}
			string path = file;
			if (needCutCurrent && path.StartsWith(currentDirectory))
				path = file.Substring(currentDirectory.Length);
			int offset = 0;
			int currentLineIndex = 0;
			foreach (int index in indices)
			{
				int lineEnd = -1;
				while (true)
				{
					int nIndex = text.IndexOf('\n', offset);
					int rIndex = text.IndexOf('\r', offset);
					if (nIndex == -1 && rIndex == -1)
					{
						lineEnd = text.Length;
						break;
					}
					int nrIndex = Math.Min(nIndex, rIndex);
					if (nrIndex == -1)
						nrIndex = nIndex != -1 ? nIndex : rIndex;
					if (nrIndex > index)
					{
						lineEnd = nrIndex;
						break;
					}
					currentLineIndex++;
					if (nrIndex == nIndex)
					{
						offset = nIndex + 1;
					}
					else
					{
						if (rIndex + 1 < text.Length || text[rIndex + 1] == '\n')
							offset = rIndex + 2;
						else
							offset = rIndex + 1;
					}
				}
				builder.AppendLine(
					path + "(" + (currentLineIndex + 1) + "," + (index - offset + 1) + "): " + text.Substring(offset, lineEnd - offset));
			}
		}
		Console.Out.Write(builder.ToString());
	}
}
