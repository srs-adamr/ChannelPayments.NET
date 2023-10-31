using RestSharp;

namespace ChannelPayments.NET.Models;

public interface IChannelPaymentsRequest
{
    string? Endpoint { get; set; }
    Method RequestType { get; set; }
}