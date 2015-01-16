using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Database.Storage
{
    public class Document
    {
        private Dictionary<string, string> _data = new Dictionary<string, string>();

        public Document(string json)
        {
            using (StringReader strReader = new StringReader(json))
            using (JsonTextReader reader = new JsonTextReader(strReader))
            {
                reader.Read();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string key = reader.Value.ToString();
                        _data.Add(key, reader.ReadAsString());
                    }
                }
            }
        }

        public bool Find(string key, string value)
        {
            return _data.ContainsKey(key) && _data[key] == value;
        }

        public string ToJson()
        {
            StringBuilder builder = new StringBuilder();

            using (StringWriter strWriter = new StringWriter(builder))
            using (JsonTextWriter writer = new JsonTextWriter(strWriter))
            {
                writer.WriteStartObject();

                foreach (var item in _data)
                {
                    writer.WritePropertyName(item.Key);
                    writer.WriteValue(item.Value);
                }

                writer.WriteEndObject();
            }

            return builder.ToString();
        }
    }
}