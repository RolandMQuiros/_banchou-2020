// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1129 // Do not use default value type constructor
#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1309 // Field names should not begin with underscore
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Banchou.Pawn
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class PawnIdFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Banchou.Pawn.PawnId>
    {


        public void Serialize(ref MessagePackWriter writer, global::Banchou.Pawn.PawnId value, global::MessagePack.MessagePackSerializerOptions options)
        {
            IFormatterResolver formatterResolver = options.Resolver;
            writer.WriteArrayHeader(1);
            writer.Write(value.Id);
        }

        public global::Banchou.Pawn.PawnId Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                throw new InvalidOperationException("typecode is null, struct not supported");
            }

            options.Security.DepthStep(ref reader);
            IFormatterResolver formatterResolver = options.Resolver;
            var length = reader.ReadArrayHeader();
            var __Id__ = default(int);

            for (int i = 0; i < length; i++)
            {
                var key = i;

                switch (key)
                {
                    case 0:
                        __Id__ = reader.ReadInt32();
                        break;
                    default:
                        reader.Skip();
                        break;
                }
            }

            var ____result = new global::Banchou.Pawn.PawnId(__Id__);
            reader.Depth--;
            return ____result;
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1129 // Do not use default value type constructor
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1309 // Field names should not begin with underscore
#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name
