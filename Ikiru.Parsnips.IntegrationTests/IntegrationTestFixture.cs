using Ikiru.Parsnips.IntegrationTests.Helpers.Infrastructure;
using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace Ikiru.Parsnips.IntegrationTests
{
    /// <summary>
    /// Runs before all Tests within the same Assembly to ensure infrastructure is running.
    /// </summary>
    public sealed class IntegrationTestFixture : IDisposable
    {
        private List<string> m_StartupLogs = new List<string>();

        public IntegrationTestFixture()
        {
            m_StartupLogs.Add("Fixture Startup...");
            var outputWriter = new Action<string>(s =>
                                                  {
                                                      m_StartupLogs.Add(s);
                                                  });
        }

        public void EnsureStartupLogsOutput(ITestOutputHelper output)
        {
            // Could make threadsafe, but XUnit Test Collection will run all in parallel
            if (m_StartupLogs == null)
                return;

            foreach (var startupLog in m_StartupLogs)
                output.WriteLine(startupLog);

            m_StartupLogs = null;
        }

        public void Dispose()
        {
        }
    }
}