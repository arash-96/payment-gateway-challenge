﻿using System.Net;
using System.Net.Http.Json;
using System.Text;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private readonly Random _random = new();
    private readonly UtilityFunctions _utilityFunctions = new();

    public static string GenerateRandomCardNumber()
    {
        var rand = new Random();
        string cardNumber = "";
        int randomLength = rand.Next(14, 20);
        for (int i = 0; i < randomLength; i++)
        {
            cardNumber += rand.Next(0, 10);
        }
        return cardNumber;
    }

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = _random.Next(2023, 2030),
            ExpiryMonth = _random.Next(1, 12),
            Amount = _random.Next(1, 10000),
            CardNumberLastFour = _random.Next(1111, 9999),
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/getpayment/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/getpayment/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostPayment_ReturnError()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(13, 20), // invalid
            ExpiryYear = _random.Next(DateTime.Now.Year - 20, DateTime.Now.Year - 10), // invalid
            Currency = "GBP", //valid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        var jsonContent = JsonConvert.SerializeObject(postPaymentRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/Payments/sendpayment", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostPayment_ReturnOk()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(1, 13), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year, DateTime.Now.Year + 10), // valid
            Currency = "USD", //valid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };              

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();

        var jsonContent = JsonConvert.SerializeObject(postPaymentRequest);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/Payments/sendpayment", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public void Validate_ReturnEmptyString()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(1, 13), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year + 1, DateTime.Now.Year + 10), // valid
            Currency = "USD", //valid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);
        bool isTrue = validate == "";

        // Assert
        Assert.True(isTrue);
    }
    [Fact]
    public void Validate_ReturnInvalidCardNumber()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = "222240534324811255555555555555555555", //invalid
            ExpiryMonth = _random.Next(1, 13), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year + 1, DateTime.Now.Year + 10), // valid
            Currency = "USD", //valid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);
        
        // Assert
        Assert.Equal("The card number is invalid", validate);
    }
    [Fact]
    public void Validate_ReturnInvalidMonth()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(13, 16), //invalid
            ExpiryYear = _random.Next(DateTime.Now.Year + 1, DateTime.Now.Year + 10), // valid
            Currency = "USD", //valid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);

        // Assert
        Assert.Equal("The expiry month is invalid", validate);
    }

    [Fact]
    public void Validate_ReturnInvalidYear()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(1, 13), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year - 5, DateTime.Now.Year - 1), // invalid
            Currency = "USD", //valid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);

        // Assert
        Assert.Equal("The expiry year is invalid", validate);
    }

    [Fact]
    public void Validate_ReturnInvalidCurrency()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(1, 13), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year + 1, DateTime.Now.Year + 10), // valid
            Currency = "YYY", //invalid
            Amount = _random.Next(1, 90000), //valid
            Cvv = _random.Next(100, 1000) //valid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);

        // Assert
        Assert.Equal("The currency is invalid", validate);
    }

    [Fact]
    public void Validate_ReturnInvalidAmount()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(1, 12), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year + 1, DateTime.Now.Year + 10), // valid
            Currency = "USD", //valid
            Amount = _random.Next(-100, 0), //invalid
            Cvv = _random.Next(100, 1000) //valid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);

        // Assert
        Assert.Equal("The amount is invalid", validate);
    }

    [Fact]
    public void Validate_ReturnInvalidCvv()
    {
        // Arrange
        var postPaymentRequest = new PostPaymentRequest // Valid object
        {
            CardNumber = GenerateRandomCardNumber(), //valid
            ExpiryMonth = _random.Next(1, 13), //valid
            ExpiryYear = _random.Next(DateTime.Now.Year + 1, DateTime.Now.Year + 10), // valid
            Currency = "USD", //valid
            Amount = _random.Next(1, 100000), //valid
            Cvv = _random.Next(10000, 100000) //invalid
        };

        // Act
        string validate = _utilityFunctions.Validate(postPaymentRequest);

        // Assert
        Assert.Equal("The Cvv is invalid", validate);
    }

}