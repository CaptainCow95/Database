namespace Database.Console
{
    /// <summary>
    /// Represents a <see cref="CommandPart"/> that is a constant string.
    /// </summary>
    public class CommandPartLiteral : CommandPart
    {
        /// <summary>
        /// The value of the constant.
        /// </summary>
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPartLiteral"/> class.
        /// </summary>
        /// <param name="value">The value of the constant.</param>
        public CommandPartLiteral(string value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the value of the constant.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <inheritdoc />
        public override bool Parse(ref string text, out CommandPart output)
        {
            if (text.StartsWith(_value))
            {
                text = text.Substring(_value.Length);
                output = new CommandPartLiteral(_value);
                return true;
            }

            output = null;
            return false;
        }
    }
}