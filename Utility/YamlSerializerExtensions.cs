﻿using System.Collections.Generic;
using System.IO;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Zipline.Utility
{
  public static class YamlSerializerExtensions
  {
    public static IEnumerable<TItem> DeserializeMany<TItem>(
        this IDeserializer deserializer,
        TextReader input)
    {
      var reader = new Parser(input);
      reader.Consume<StreamStart>();

      while (reader.TryConsume<DocumentStart>(out var dummyStart))
      {
        var item = deserializer.Deserialize<TItem>(reader);
        yield return item;
        reader.TryConsume<DocumentEnd>(out var dummyEnd);
      }
    }
  }
}
