namespace CSMOO.Logging;

/// <summary>
/// Interface for logging functionality
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Initialize logging system and rotate logs if needed
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Logs a debug message (only when debug mode is enabled)
    /// </summary>
    void Debug(string message);
    
    /// <summary>
    /// Logs an informational message
    /// </summary>
    void Info(string message);
    
    /// <summary>
    /// Logs a warning message
    /// </summary>
    void Warning(string message);
    
    /// <summary>
    /// Logs an error message
    /// </summary>
    void Error(string message);
    
    /// <summary>
    /// Logs an error with exception details
    /// </summary>
    void Error(string message, Exception ex);
    
    /// <summary>
    /// Logs a game-related message (always goes to game.log)
    /// </summary>
    void Game(string message);
    
    /// <summary>
    /// Display a stylized startup banner
    /// </summary>
    void DisplayBanner();
    
    /// <summary>
    /// Display a stylized section header
    /// </summary>
    void DisplaySectionHeader(string title);
}
