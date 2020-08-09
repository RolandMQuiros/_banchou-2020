// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Linq;
// using System.Linq.Expressions;
// using System.Reflection;

// using MessagePack;

// using Banchou.Network.Message;

// namespace Banchou.Network {
//     public class ActionConverter {
//         private MessagePackSerializerOptions _serializerOptions;
//         private Dictionary<string, Func<byte[], object>> _actionConverters;

//         public ActionConverter(MessagePackSerializerOptions serializerOptions) {
//             _serializerOptions = serializerOptions;

//             _actionConverters = AppDomain.CurrentDomain
//                 .GetAssemblies()
//                 .SelectMany(assembly => assembly.GetTypes())
//                 .Where(type => type.Namespace?.EndsWith("StateAction") == true)
//                 .ToDictionary(
//                     type => type.ToString(),
//                     type => {
//                         var methodInfo = typeof(ActionConverter)
//                             .GetMethod("DeserializeAction", BindingFlags.Instance | BindingFlags.NonPublic)
//                             .MakeGenericMethod(type);
//                         var input = Expression.Parameter(typeof(byte[]));
//                         var call = Expression.Convert(
//                             Expression.Call(Expression.Constant(this), methodInfo, input),
//                             typeof(object)
//                         );

//                         return Expression.Lambda<Func<byte[], object>>(call, input).Compile();
//                     }
//                 );
//         }

//         public void SerializeToEnvelope(MemoryStream memoryStream, object action) {
//             MessagePackSerializer.Serialize(
//                 memoryStream,
//                 new Envelope {
//                     PayloadType = PayloadType.ReduxAction,
//                     Payload = MessagePackSerializer.Serialize(
//                         new ReduxAction {
//                             ActionType = action.GetType().ToString(),
//                             ActionData = MessagePackSerializer.Serialize(
//                                 action,
//                                 _serializerOptions
//                             )
//                         },
//                         _serializerOptions
//                     )
//                 },
//                 _serializerOptions
//             );
//         }

//         public object Deserialize(byte[] actionBytes, string typeName) {
//             Func<byte[], object> converter;
//             if (_actionConverters.TryGetValue(typeName, out converter)) {
//                 return converter?.Invoke(actionBytes);
//             }
//             return null;
//         }

//         private T DeserializeAction<T>(byte[] data) {
//             return MessagePackSerializer.Deserialize<T>(data, _serializerOptions);
//         }
//     }
// }