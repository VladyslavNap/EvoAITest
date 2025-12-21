namespace EvoAITest.Core.Models.Vision;

/// <summary>
/// Defines the type of UI element detected in a screenshot.
/// </summary>
public enum ElementType
{
    /// <summary>
    /// Unknown or unclassified element.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Button element (clickable).
    /// </summary>
    Button = 1,

    /// <summary>
    /// Text input field.
    /// </summary>
    Input = 2,

    /// <summary>
    /// Textarea element for multi-line text.
    /// </summary>
    Textarea = 3,

    /// <summary>
    /// Dropdown/select element.
    /// </summary>
    Select = 4,

    /// <summary>
    /// Checkbox element.
    /// </summary>
    Checkbox = 5,

    /// <summary>
    /// Radio button element.
    /// </summary>
    Radio = 6,

    /// <summary>
    /// Hyperlink element.
    /// </summary>
    Link = 7,

    /// <summary>
    /// Image element.
    /// </summary>
    Image = 8,

    /// <summary>
    /// Text label or span.
    /// </summary>
    Label = 9,

    /// <summary>
    /// Heading element (h1-h6).
    /// </summary>
    Heading = 10,

    /// <summary>
    /// Icon or glyph.
    /// </summary>
    Icon = 11,

    /// <summary>
    /// Card or panel container.
    /// </summary>
    Card = 12,

    /// <summary>
    /// Navigation menu or bar.
    /// </summary>
    Menu = 13,

    /// <summary>
    /// Modal or dialog.
    /// </summary>
    Dialog = 14,

    /// <summary>
    /// Table element.
    /// </summary>
    Table = 15,

    /// <summary>
    /// List (ordered or unordered).
    /// </summary>
    List = 16,

    /// <summary>
    /// Form element.
    /// </summary>
    Form = 17,

    /// <summary>
    /// Search box or search input.
    /// </summary>
    SearchBox = 18,

    /// <summary>
    /// Toggle switch.
    /// </summary>
    Toggle = 19,

    /// <summary>
    /// Slider or range input.
    /// </summary>
    Slider = 20
}
