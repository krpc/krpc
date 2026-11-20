namespace KRPC.Server.HTTP
{
    /// <summary>
    /// A JSON number, kept as its text so that an integer converts without passing through
    /// a double.
    /// </summary>
    sealed class JsonNumber
    {
        public JsonNumber (string text)
        {
            Text = text;
        }

        public string Text { get; private set; }
    }
}
