// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
    using System;

    public class BanchouMessagePackResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new BanchouMessagePackResolver();

        private BanchouMessagePackResolver()
        {
        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var f = BanchouMessagePackResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class BanchouMessagePackResolverGetFormatterHelper
    {
        private static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;

        static BanchouMessagePackResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(13)
            {
                { typeof(global::Banchou.Network.Message.PayloadType), 0 },
                { typeof(global::Banchou.Player.InputCommand), 1 },
                { typeof(global::Banchou.Player.InputUnitType), 2 },
                { typeof(global::Banchou.Network.Message.ConnectClient), 3 },
                { typeof(global::Banchou.Network.Message.Envelope), 4 },
                { typeof(global::Banchou.Network.Message.ReduxAction), 5 },
                { typeof(global::Banchou.Network.Message.ServerTimeRequest), 6 },
                { typeof(global::Banchou.Network.Message.ServerTimeResponse), 7 },
                { typeof(global::Banchou.Network.Message.SyncClient), 8 },
                { typeof(global::Banchou.Network.Message.SyncPawn), 9 },
                { typeof(global::Banchou.Pawn.PawnId), 10 },
                { typeof(global::Banchou.Player.InputUnit), 11 },
                { typeof(global::Banchou.Player.PlayerId), 12 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key))
            {
                return null;
            }

            switch (key)
            {
                case 0: return new MessagePack.Formatters.Banchou.Network.Message.PayloadTypeFormatter();
                case 1: return new MessagePack.Formatters.Banchou.Player.InputCommandFormatter();
                case 2: return new MessagePack.Formatters.Banchou.Player.InputUnitTypeFormatter();
                case 3: return new MessagePack.Formatters.Banchou.Network.Message.ConnectClientFormatter();
                case 4: return new MessagePack.Formatters.Banchou.Network.Message.EnvelopeFormatter();
                case 5: return new MessagePack.Formatters.Banchou.Network.Message.ReduxActionFormatter();
                case 6: return new MessagePack.Formatters.Banchou.Network.Message.ServerTimeRequestFormatter();
                case 7: return new MessagePack.Formatters.Banchou.Network.Message.ServerTimeResponseFormatter();
                case 8: return new MessagePack.Formatters.Banchou.Network.Message.SyncClientFormatter();
                case 9: return new MessagePack.Formatters.Banchou.Network.Message.SyncPawnFormatter();
                case 10: return new MessagePack.Formatters.Banchou.Pawn.PawnIdFormatter();
                case 11: return new MessagePack.Formatters.Banchou.Player.InputUnitFormatter();
                case 12: return new MessagePack.Formatters.Banchou.Player.PlayerIdFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1649 // File name should match first type name
