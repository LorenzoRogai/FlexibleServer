using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

public enum LogLevel
{
    Debug = 0,
    Information = 1,
    Warning = 2,
    Error = 3,
    Success = 4
}

public class Logging
{
    public LogLevel MinimumLogLevel; //The minimum log level to display

    /// <summary>
    /// Clear Console
    /// </summary>
    public void Clear()
    {
        Console.Clear();
    }

    /// <summary>
    /// Write Line to Console
    /// <param name="Line">Line text</param>     
    /// </summary>
    public void WriteLine(string Line)
    {
        WriteLine(Line, LogLevel.Information);
    }

    /// <summary>
    /// Write Line to Console with specified Log Level
    /// <param name="Line">Line text</param>
    /// <param name="Level">LogLevel</param>
    /// </summary>
    public void WriteLine(string Line, LogLevel Level)
    {
        if (Level >= MinimumLogLevel) //Don't write line to Console if LogLevel is lower than MinimumLogLevel
        {
            DateTime _DTN = DateTime.Now;
            StackFrame _SF = new StackTrace().GetFrame(1); 
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(_SF.GetMethod().ReflectedType.Name + "." + _SF.GetMethod().Name); //Write current Class.Method
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("] » ");

            //Change color based on log level
            if (Level == LogLevel.Debug)
                Console.ForegroundColor = ConsoleColor.Gray;
            else if (Level == LogLevel.Error)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (Level == LogLevel.Information)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (Level == LogLevel.Success)
                Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(Line);
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}