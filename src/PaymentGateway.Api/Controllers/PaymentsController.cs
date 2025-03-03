﻿using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;    
    private readonly UtilityFunctions _utilityFunctions;    

    public PaymentsController(PaymentsRepository paymentsRepository, UtilityFunctions utilityFunctions)
    {
        _paymentsRepository = paymentsRepository;
        _utilityFunctions = utilityFunctions;        
    }

    [HttpGet("getpayment/{id:guid}")]
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

    [HttpPost("sendpayment")]
    public async Task<ActionResult<PostPaymentResponse>> PostPayment([FromBody] PostPaymentRequest request)
    {       
        //Define payment ID
        Guid paymentID = Guid.NewGuid();

        PostPaymentResponse paymentDetails;

        //Validate Data
        string isValidated = _utilityFunctions.Validate(request);      
        if (isValidated != "") 
        {            
            //Add Rejected request to the DB?
            //paymentDetails = _utilityFunctions.GeneratePostPaymentDBItem(request, PaymentStatus.Rejected, paymentID);
            
            return BadRequest(isValidated);
        }

        //Bank Response
        var bankResponse = await _utilityFunctions.PostToBank(request);
                
        if (bankResponse != null && bankResponse.Authorized)
        {
            paymentDetails = _utilityFunctions.GeneratePostPaymentDBItem(request, PaymentStatus.Authorized, paymentID);
        }
        else
        {
            paymentDetails = _utilityFunctions.GeneratePostPaymentDBItem(request, PaymentStatus.Declined, paymentID);
        }

        //Add Data to DB
        _paymentsRepository.Add(paymentDetails);

        return Ok(paymentDetails);
    }
}