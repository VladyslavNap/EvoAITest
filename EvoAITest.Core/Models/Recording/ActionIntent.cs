namespace EvoAITest.Core.Models.Recording;

/// <summary>
/// Represents the detected intent behind a user action
/// </summary>
public enum ActionIntent
{
    /// <summary>
    /// Unknown or unclear intent
    /// </summary>
    Unknown,
    
    /// <summary>
    /// User authentication (login, logout)
    /// </summary>
    Authentication,
    
    /// <summary>
    /// Searching for content
    /// </summary>
    Search,
    
    /// <summary>
    /// Navigating to a different page or section
    /// </summary>
    Navigation,
    
    /// <summary>
    /// Submitting a form
    /// </summary>
    FormSubmission,
    
    /// <summary>
    /// Entering or modifying data
    /// </summary>
    DataEntry,
    
    /// <summary>
    /// Selecting or filtering data
    /// </summary>
    Selection,
    
    /// <summary>
    /// Validating displayed information
    /// </summary>
    Validation,
    
    /// <summary>
    /// Creating new content or records
    /// </summary>
    Creation,
    
    /// <summary>
    /// Updating existing content
    /// </summary>
    Update,
    
    /// <summary>
    /// Deleting content
    /// </summary>
    Deletion,
    
    /// <summary>
    /// Downloading files or data
    /// </summary>
    Download,
    
    /// <summary>
    /// Uploading files
    /// </summary>
    Upload,
    
    /// <summary>
    /// Interacting with modal dialogs
    /// </summary>
    DialogInteraction,
    
    /// <summary>
    /// Waiting for content to load
    /// </summary>
    Waiting,
    
    /// <summary>
    /// Verifying error messages or validation
    /// </summary>
    ErrorVerification
}
