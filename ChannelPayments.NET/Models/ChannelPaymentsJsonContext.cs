using System.Text.Json.Serialization;

namespace ChannelPayments.NET;

[JsonSerializable(typeof(TestRequest))]
[JsonSerializable(typeof(TestResponse))]
public partial class ChannelPaymentsJsonContext : JsonSerializerContext
{

}