using Database.Common.DataOperation;
using System;

namespace Database.Common.Messages
{
    /// <summary>
    /// Represents the result of a <see cref="DataOperation"/> message.
    /// </summary>
    public class DataOperationResult : BaseMessageData
    {
        /// <summary>
        /// The result of the operation.
        /// </summary>
        private readonly string _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperationResult"/> class.
        /// </summary>
        /// <param name="doc">The result of the operation.</param>
        /// <param name="addSuccess">A value indicating whether to add the success and result fields.</param>
        public DataOperationResult(Document doc, bool addSuccess = true)
        {
            if (addSuccess)
            {
                _result = "{\"success\":true,\"result\":" + doc.ToJson() + "}";
            }
            else
            {
                _result = doc.ToJson();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperationResult"/> class.
        /// </summary>
        /// <param name="errorCode">The error code to send.</param>
        /// <param name="errorDescription">The error description to send.</param>
        public DataOperationResult(ErrorCodes errorCode, string errorDescription)
        {
            _result = "{\"success\":false,\"errorcode\":\"" + Enum.GetName(typeof(ErrorCodes), errorCode) + "\",\"errordescription\":\"" + errorDescription + "\"}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperationResult"/> class.
        /// </summary>
        /// <param name="previousResult">The <see cref="DataOperationResult"/> to copy values from.</param>
        public DataOperationResult(DataOperationResult previousResult)
        {
            _result = previousResult._result;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataOperationResult"/> class.
        /// </summary>
        /// <param name="data">The data to read from.</param>
        /// <param name="index">The index at which to start reading from.</param>
        internal DataOperationResult(byte[] data, int index)
        {
            _result = ByteArrayHelper.ToString(data, ref index);
        }

        /// <summary>
        /// Gets the result of the operation.
        /// </summary>
        public string Result
        {
            get { return _result; }
        }

        /// <inheritdoc />
        protected override byte[] EncodeInternal()
        {
            return ByteArrayHelper.Combine(ByteArrayHelper.ToBytes(_result));
        }

        /// <inheritdoc />
        protected override int GetMessageTypeId()
        {
            return (int)MessageType.DataOperationResult;
        }
    }
}