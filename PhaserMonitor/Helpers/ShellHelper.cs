using System;
using System.Diagnostics;
public static class ShellHelper
{
    public static string Bash(this string cmd, bool captureErrorOutput = false)
    {
        var escapedArgs = cmd.Replace("\"", "\\\"");

        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{escapedArgs}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = captureErrorOutput,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };
        process.Start();
        string result = process.StandardOutput.ReadToEnd();
        if (captureErrorOutput)
        {
            result += process.StandardError.ReadToEnd();
        }
        process.WaitForExit();
        return result;
    }
}
