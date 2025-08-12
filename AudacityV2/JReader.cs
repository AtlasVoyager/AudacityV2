using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudacityV2
{
    internal class JReader
    {
        public string? token { get; set; }
        public string? prefix { get; set; }

        public async Task JRead()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                string json = await sr.ReadToEndAsync();
                JStruct? data = JsonConvert.DeserializeObject<JStruct>(json);

                if (data == null)
                    throw new InvalidOperationException("Failed to deserialize config.json to JStruct.");

                token = data.token;
                prefix = data.prefix;
            }
        }
    }

    internal sealed class JStruct
    {
        public string? token { get; set; }
        public string? prefix { get; set; }
    }
}
