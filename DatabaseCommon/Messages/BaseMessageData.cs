using System;

namespace Database.Common.Messages
{
    /// <summary>
    /// The base class for the different message types.
    /// </summary>
    public abstract class BaseMessageData
    {
        /// <summary>
        /// Decodes the byte array into the message's data.
        /// </summary>
        /// <param name="data">The data to decode.</param>
        /// <param name="index">The index in the byte array to start at.</param>
        /// <returns>The message data that was decoded.</returns>
        public static BaseMessageData Decode(byte[] data, int index)
        {
            int messageTypeId = ByteArrayHelper.ToInt32(data, ref index);
            MessageType messageTypeIdConverted = (MessageType)Enum.ToObject(typeof(MessageType), messageTypeId);
            switch (messageTypeIdConverted)
            {
                case MessageType.JoinAttempt:
                    return new JoinAttempt(data, index);

                case MessageType.JoinSuccess:
                    return new JoinSuccess(data, index);

                case MessageType.JoinFailure:
                    return new JoinFailure(data, index);

                case MessageType.Heartbeat:
                    return new Heartbeat();

                default:
                    throw new Exception("Message type id not found: " + messageTypeId);
            }
        }

        /// <summary>
        /// Encodes the message data into a byte array.
        /// </summary>
        /// <returns>The encoded data as a byte array.</returns>
        public byte[] Encode()
        {
            return ByteArrayHelper.Combine(BitConverter.GetBytes(GetMessageTypeId()), EncodeInternal());
        }

        /// <summary>
        /// Encodes the message data into a byte array.
        /// </summary>
        /// <returns>The encoded data as a byte array.</returns>
        /// <remarks>This is the overridden method that is called internally by the Encode method.</remarks>
        public abstract byte[] EncodeInternal();

        /// <summary>
        /// Gets the message type id.
        /// </summary>
        /// <returns>The id of the message type.</returns>
        protected abstract int GetMessageTypeId();
    }
}