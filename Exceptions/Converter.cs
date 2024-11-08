using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Exceptions;

public class Converter
{
    private static readonly Logger log = LogManager.GetCurrentClassLogger();

    public static void ConvertFiles(params string[] filenames)
    {
        try
        {
            filenames = filenames.Length > 0 ? filenames : new[] { "text.txt" };
            var settings = LoadSettings();
            ConvertFiles(filenames, settings);
        }
        catch (Exception e)
        {
            log.Error(e);
        }
    }

    private static void ConvertFiles(string[] filenames, Settings settings)
    {
        var tasks = filenames
            .Select(fn => Task.Run(() => ConvertFile(fn, settings)))
            .ToArray();
        Task.WaitAll(tasks);
    }

    private static Settings LoadSettings()
    {
        using var stream = new FileStream("settings.json", FileMode.Open);
        try
        {
            return JsonSerializer.Deserialize<Settings>(stream);
        }
        catch (Exception e)
        {
            throw new JsonException("Не удалось прочитать файл настроек");
        }
    }

    private static void ConvertFile(string filename, Settings settings)
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo(settings.SourceCultureName);
        if (settings.Verbose)
        {
            log.Info("Processing file " + filename);
            log.Info("Source Culture " + Thread.CurrentThread.CurrentCulture.Name);
        }
        IEnumerable<string> lines;
        lines = PrepareLines(filename);
        try
        {
            var convertedLines = lines
                .Select(ConvertLine)
                .Select(s => s.Length + " " + s);
            File.WriteAllLines(filename + ".out", convertedLines);
        }
        catch (Exception e)
        {
            //throw new FileNotFoundException();
            log.Error($"Не удалось сконвертировать {filename}", e);
        }
    }

    private static IEnumerable<string> PrepareLines(string filename)
    {
        var lineIndex = 0;
        foreach (var line in File.ReadLines(filename))
        {
            if (line == string.Empty) continue;
            yield return line.Trim();
            lineIndex++;
        }
        yield return lineIndex.ToString();
    }

    public static string ConvertLine(string arg)
    {
        try
        {
            return ConvertAsDateTime(arg);
        }
        catch
        {
            try
            {
                return ConvertAsDouble(arg);
            }
            catch
            {
                return ConvertAsCharIndexInstruction(arg);
            }
        }
    }

    private static string ConvertAsCharIndexInstruction(string s)
    {
        var parts = s.Split();
        if (parts.Length < 2) 
            throw new Exception("Некорректная строка");
        var charIndex = int.Parse(parts[0]);
        if ((charIndex < 0) || (charIndex >= parts[1].Length))
            throw new Exception("Некорректная строка");
        var text = parts[1];
        return text[charIndex].ToString();
    }

    private static string ConvertAsDateTime(string arg)
    {
        DateTime date = new DateTime();
        if (!DateTime.TryParse(arg, out date)) 
            throw new Exception("Некорректная строка");
        return date.ToString(CultureInfo.InvariantCulture);
    }

    private static string ConvertAsDouble(string arg)
    {
        return double.Parse(arg).ToString(CultureInfo.InvariantCulture);
    }
}
