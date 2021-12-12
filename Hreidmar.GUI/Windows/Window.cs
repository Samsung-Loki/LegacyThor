namespace Hreidmar.GUI.Windows
{
    /// <summary>
    /// Hreidmar GUI window
    /// </summary>
    public abstract class Window
    {
        private bool _isOpened;

        /// <summary>
        /// Open the window
        /// </summary>
        public virtual void Open()
            => _isOpened = true;

        /// <summary>
        /// Close the window
        /// </summary>
        public virtual void Close()
            => _isOpened = false;

        /// <summary>
        /// Is the window closed
        /// </summary>
        /// <returns>Value</returns>
        public virtual bool IsOpened()
            => _isOpened;

        /// <summary>
        /// Draw the window
        /// </summary>
        public virtual void Draw() { }
    }
}