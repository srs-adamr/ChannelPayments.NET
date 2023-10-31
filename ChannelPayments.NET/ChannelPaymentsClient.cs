using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using ChannelPayments.NET.Models;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using RestSharp.Serializers.Json;

namespace ChannelPayments.NET
{
    public class ChannelPaymentsClient
    {
        private readonly RestClient _restClient;

        public ChannelPaymentsClient(string apiKey, bool useSandbox = false, int maxTimeout = 20000)
        {
            // Replace these with your actual details
            var issuer = "your-issuer";
            var keyId = "your-key-id"; // Key ID from your API dashboard
            var baseUrl = useSandbox ? "https://sandbox.channelpayments-api.com" : "  https://channelpayments-app.com"; // API endpoint

            // Read the private key from the PEM file
            var privateKeyFilePath = @"path-to-your-secretKey.pem"; // Replace with your actual PEM file path
            var privateKeyText = File.ReadAllText(privateKeyFilePath);

            // Convert the PEM-encoded private key to an ECDsa object
            ECDsa privateKey = ECDsa.Create();
            byte[] privateKeyBits = Convert.FromBase64String(privateKeyText);
            privateKey.ImportECPrivateKey(privateKeyBits, out _);

            // Set the key ID (kid) and instantiate signing credentials with the ECDsa private key
            var signingCredentials = new SigningCredentials(new ECDsaSecurityKey(privateKey) { KeyId = keyId },
                SecurityAlgorithms.EcdsaSha512);

            // Prepare the claims for your JWT
            var currentTime = DateTime.UtcNow;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = issuer,
                NotBefore = currentTime,
                Expires = currentTime.AddMinutes(5), // Tokens should be short-lived
                SigningCredentials = signingCredentials
            };

            // Create the JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = tokenHandler.WriteToken(securityToken);

            // Use RestSharp to make the authenticated request
            _restClient = new RestClient(
                new RestClientOptions
                {

                    MaxTimeout = maxTimeout,
                    ThrowOnDeserializationError = true,
                    ThrowOnAnyError = true,
                    BaseUrl = new Uri(baseUrl)

                },
                configureSerialization: s => s.UseSystemTextJson(new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                })
            );

            var request = new RestRequest("", Method.Get);
            request.AddHeader("Authorization", $"Bearer {jwtToken}");
            request.AddHeader("Content-Type", "application/json");

            // Execute the request
            var response = _restClient.Execute(request);

            // Handle the response
            if (response.IsSuccessful)
            {
                Console.WriteLine("Response received successfully.");
                Console.WriteLine(response.Content);
            }
            else
            {
                Console.WriteLine("Error occurred: " + response.ErrorMessage);
            }

        }

        public async Task<T?> SendRequest<T>(IChannelPaymentsRequest request) where T : class, IChannelPaymentsResponse
        {
            // Create a RestRequest with the specified endpoint and method
            var restRequest = new RestRequest(request.Endpoint, request.RequestType);

            // Serialize the request using the source-generated context for the specific type of 'request'
            string requestBodyJson = JsonSerializer.Serialize(request, ChannelPaymentsJsonContext.Default.Options);


            // Add the serialized request body to the RestRequest
            restRequest.AddJsonBody(requestBodyJson);

            // Execute the request asynchronously
            var response = await _restClient.ExecuteAsync(restRequest);


            T? result = JsonSerializer.Deserialize<T>(response.Content, ChannelPaymentsJsonContext.Default.Options);
            return result;

        }

    }
    

    public class TestRequest : IChannelPaymentsRequest
    {
        public string? Endpoint { get; set; }
        public Method RequestType { get; set; }
    }
  
    public class TestResponse : IChannelPaymentsResponse
    {

    }
}
