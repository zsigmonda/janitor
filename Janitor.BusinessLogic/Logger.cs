using System;
using System.IO;

namespace Janitor.BusinessLogic
{
  /// <summary>
  /// Ez az osztály felel a naplózásért. Konkurens singleton mintát használ.
  /// </summary>
  public class Logger
  {
    private readonly static Object m_lockObject = new Object();

    private static Logger m_instance;
    /// <summary>
    /// Ez a property visszaadja az osztály egyetlen példányát.
    /// </summary>
    public static Logger Instance
    {
      get
      {
        if (m_instance == null)
        {
          lock (m_lockObject)
          {
            if (m_instance == null)
            {
              m_instance = new Logger();
            }
          }
        }
        return m_instance;
      }
    }

    private StreamWriter m_logWriter = null;

    /// <summary>
    /// Új példány létrehozása és inicializálása. A konstruktor kívülről nem elérhető, mivel ez egy singleton osztály.
    /// </summary>
    private Logger()
    {
      try
      {
        m_logWriter = new StreamWriter(Path.Combine(Path.GetTempPath(), "Janitor_log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log"));
        m_logWriter.WriteLine("Janitor Visual Studio Extension - Logging started\n");
        m_logWriter.Flush();
      }
      catch (Exception)
      {
        m_logWriter = null;
      }
    }

    /// <summary>
    /// Hibaüzenet naplózása a naplófájlba.
    /// </summary>
    /// <param name="message">A hibaüzenet szövege.</param>
    /// <param name="exception">Kapcsolódó kivétel.</param>
    public void LogError(string message, Exception exception = null)
    {
      if (m_logWriter != null)
      {
        if (exception == null)
        {
          m_logWriter.WriteLine("{0}\tERROR\t{1}\tException details missing.", DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss"), message);
        }
        else
        {
          m_logWriter.WriteLine("{0}\tERROR\t{1}\t{2}\n{3}", DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss"), message, exception.Message, exception.StackTrace);
        }
        m_logWriter.Flush();
      }
    }

    /// <summary>
    /// Figyelmeztető üzenet írása a naplófájlba.
    /// </summary>
    /// <param name="message">Az üzenet szövege.</param>
    public void LogWarning(string message)
    {
      if (m_logWriter != null)
      {
        m_logWriter.WriteLine("{0}\tWARN\t{1}", DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss"), message);
        m_logWriter.Flush();
      }
    }

    /// <summary>
    /// Tájékoztató üzenet írása a naplófájlba.
    /// </summary>
    /// <param name="message">Az üzenet szövege.</param>
    public void LogInformation(string message)
    {
      if (m_logWriter != null)
      {
        m_logWriter.WriteLine("{0}\tINFO\t{1}", DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss"), message);
        m_logWriter.Flush();
      }
    }
  }
}
