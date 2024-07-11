using System;
using System.IO;

namespace MicrosoftTeamsIntegration.Jira.Tests;

public sealed class ConsoleOutput : IDisposable
{
    private readonly StringWriter _stringWriter;
    private TextWriter _originalOutput;
    private bool _disposed;
    public ConsoleOutput()
    {
        _stringWriter = new StringWriter();
        _originalOutput = Console.Out;
        Console.SetOut(_stringWriter);
    }

    public string GetOutput()
    {
        return _stringWriter.ToString();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _stringWriter?.Dispose();
            }

            // Restore original console output
            Console.SetOut(_originalOutput);
            _originalOutput = null;
            _disposed = true;
        }
    }

    ~ConsoleOutput()
    {
        Dispose(false);
    }
}
