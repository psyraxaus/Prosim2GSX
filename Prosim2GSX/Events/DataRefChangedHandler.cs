namespace Prosim2GSX.Events
{
    /// <summary>
    /// Delegate for handling dataref change events
    /// </summary>
    /// <param name="dataRef">The dataref that changed</param>
    /// <param name="oldValue">The previous value</param>
    /// <param name="newValue">The new value</param>
    public delegate void DataRefChangedHandler(string dataRef, dynamic oldValue, dynamic newValue);
}