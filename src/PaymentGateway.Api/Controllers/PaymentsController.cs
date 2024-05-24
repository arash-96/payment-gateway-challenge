using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

using static PaymentGateway.Api.Services.ValidatePaymentRequest;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;    

    public PaymentsController(PaymentsRepository paymentsRepository)
    {
        _paymentsRepository = paymentsRepository;        
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PostPaymentResponse?> GetPayment(Guid id)
    {
        //Get Payment
        var payment = _paymentsRepository.Get(id);

        //If payment Not found
        if (payment == null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPayment([FromBody] PostPaymentRequest request)
    {
        UtilityFunctions ValidateFunctions = new UtilityFunctions();

        //Define payment ID
        Guid paymentID = Guid.NewGuid();

        //Validate Data
        bool isValidated = ValidateFunctions.ValidatePaymentDetails(request);      
        if (!isValidated)
        {
            object response = ValidateFunctions.GeneratePostPaymentResponse(request, PaymentStatus.Rejected, paymentID);
            return Ok(response);
        }

        //Bank Response
        var bankResponse = await ValidateFunctions.PostToBank(request);

        PostPaymentResponse paymentDetails;
        if (bankResponse != null && bankResponse.Authorized)
        {
            paymentDetails = ValidateFunctions.GeneratePostPaymentDBItem(request, PaymentStatus.Authorized, paymentID);
        }
        else
        {
            paymentDetails = ValidateFunctions.GeneratePostPaymentDBItem(request, PaymentStatus.Declined, paymentID);
        }

        //Add Data to DB
        _paymentsRepository.Add(paymentDetails);

        return Ok(paymentDetails);
    }
}