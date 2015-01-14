namespace Database.Console
{
    /// <summary>
    /// Represents a <see cref="CommandPart"/> that is made up of a string.
    /// </summary>
    public class CommandPartString : CommandPart
    {
        /// <summary>
        /// The value of the string.
        /// </summary>
        private string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPartString"/> class.
        /// </summary>
        public CommandPartString()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPartString"/> class.
        /// </summary>
        /// <param name="contents">The contents of the string.</param>
        private CommandPartString(string contents)
        {
            _value = contents;
        }

        /// <summary>
        /// Gets the value of the string.
        /// </summary>
        public string Value
        {
            get { return _value; }
        }

        /// <inheritdoc />
        public override bool Parse(ref string text, out CommandPart output)
        {
            int endIndex = -1;
            if (text.StartsWith("\""))
            {
                for (int i = 1; i < text.Length; ++i)
                {
                    if (text[i] == '"' && text[i - 1] != '\\')
                    {
                        endIndex = i;
                        break;
                    }
                }

                if (endIndex == -1)
                {
                    output = null;
                    return false;
                }

                output = new CommandPartLiteral(text.Substring(1, endIndex - 1));
                text = text.Substring(endIndex + 1);
                return true;
            }

            for (int i = 0; i < text.Length; ++i)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    endIndex = i - 1;
                }
            }

            if (endIndex == -1)
            {
                output = new CommandPartString(text);
                text = string.Empty;
            }
            else
            {
                output = new CommandPartLiteral(text.Substring(0, endIndex));
                text = text.Substring(endIndex);
            }

            return true;
        }
    }
}