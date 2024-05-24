using System.Globalization;
using System.Text.Json;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository
{
    public List<PostPaymentResponse> Payments = new();
    
    public void Add(PostPaymentResponse payment)
    {
        Payments.Add(payment);
    }

    public PostPaymentResponse Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}

public class ValidatePaymentRequest
{
    // List of valid ISO currency codes
    private static readonly string[] ValidCurrencyCodes = { "USD", "EUR", "GBP" };

    public bool Validate(PostPaymentRequest request)
    {
        bool validated = true;

        // Card Number validation
        if (string.IsNullOrWhiteSpace(request.CardNumber) ||
            request.CardNumber.Length < 14 ||
            request.CardNumber.Length > 19 ||
            !request.CardNumber.All(char.IsDigit))
        {
            validated = false;
        }

        // Expiry Month validation
        if (request.ExpiryMonth < 1 || request.ExpiryMonth > 12)
        {
            validated = false;
        }

        // Expiry Year validation
        if (request.ExpiryYear < DateTime.Now.Year ||
            (request.ExpiryYear == DateTime.Now.Year && request.ExpiryMonth < DateTime.Now.Month))
        {
            validated = false;
        }

        // Currency validation
        if (string.IsNullOrWhiteSpace(request.Currency) ||
            request.Currency.Length != 3 ||
            !ValidCurrencyCodes.Contains(request.Currency.ToUpper(CultureInfo.InvariantCulture)))
        {
            validated = false;
        }

        // Amount validation
        if (request.Amount <= 0 || request.Amount % 1 != 0)
        {
            validated = false;
        }

        // CVV validation
        if (request.Cvv.ToString().Length < 3 || request.Cvv.ToString().Length > 4 ||
            !request.Cvv.ToString().All(char.IsDigit))
        {
            validated = false;
        }

        return validated;
    }

    public class UtilityFunctions
    {
    public bool ValidatePaymentDetails(PostPaymentRequest request)
        {
            ValidatePaymentRequest validator = new ValidatePaymentRequest();
            bool isValid = validator.Validate(request);

            if (!isValid)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public object GeneratePostPaymentResponse(PostPaymentRequest request, Enum paymentStatus, Guid paymentID)
        {
            var responsePayment = new GetPaymentResponse
            {
                Id = paymentID,
                Status = paymentStatus.ToString(),
                CardNumberLastFour = Convert.ToInt32(request.CardNumber.Substring(request.CardNumber.Length - 4)),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
            };

            return responsePayment;
        }

        public PostPaymentResponse GeneratePostPaymentDBItem(PostPaymentRequest request, Enum paymentStatus, Guid paymentID)
        {
            var responsePayment = new PostPaymentResponse
            {
                Id = paymentID,
                Status = paymentStatus.ToString(),
                CardNumberLastFour = Convert.ToInt32(request.CardNumber.Substring(request.CardNumber.Length - 4)),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount,
            };

            return responsePayment;
        }


        public async Task<AuthorizationResponse?> PostToBank(PostPaymentRequest request)
        {
            var jsonRequest = SerialiseToPost(request);

            var client = new HttpClient();
            var url = "http://localhost:8080/payments";

            var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

            //Response
            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            // Deserialize the JSON content
            AuthorizationResponse? authorizationResponse = JsonSerializer.Deserialize<AuthorizationResponse>(responseContent);

            return authorizationResponse;
        }

        public string SerialiseToPost(PostPaymentRequest request)
        {
            var jsonRequest = JsonSerializer.Serialize(new
            {
                card_number = request.CardNumber.ToString(),
                expiry_date = $"{request.ExpiryMonth:d2}/{request.ExpiryYear}",
                currency = request.Currency,
                amount = request.Amount,
                cvv = request.Cvv.ToString()
            });

            return jsonRequest;
        }

    }
}
