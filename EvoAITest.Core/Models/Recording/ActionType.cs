namespace EvoAITest.Core.Models.Recording;

/// <summary>
/// Types of user actions that can be recorded during interaction
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Click action on an element
    /// </summary>
    Click,
    
    /// <summary>
    /// Double-click action on an element
    /// </summary>
    DoubleClick,
    
    /// <summary>
    /// Right-click (context menu) action
    /// </summary>
    RightClick,
    
    /// <summary>
    /// Text input or modification
    /// </summary>
    Input,
    
    /// <summary>
    /// Keyboard key press
    /// </summary>
    KeyPress,
    
    /// <summary>
    /// Page navigation
    /// </summary>
    Navigation,
    
    /// <summary>
    /// Hover over an element
    /// </summary>
    Hover,
    
    /// <summary>
    /// Scroll action
    /// </summary>
    Scroll,
    
    /// <summary>
    /// Select from dropdown
    /// </summary>
    Select,
    
    /// <summary>
    /// Checkbox or radio button toggle
    /// </summary>
    Toggle,
    
    /// <summary>
    /// Drag and drop operation
    /// </summary>
    DragDrop,
    
    /// <summary>
    /// File upload
    /// </summary>
    FileUpload,
    
    /// <summary>
    /// Form submission
    /// </summary>
    Submit,
    
    /// <summary>
    /// Window resize
    /// </summary>
    Resize,
    
    /// <summary>
    /// Wait or delay
    /// </summary>
    Wait
}
