using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Data;
using BTCPayServer.Lightning;
using BTCPayServer.Models;
using BTCPayServer.Payments;
using BTCPayServer.Payments.Lightning;
using BTCPayServer.Services.Invoices;
using BTCPayServer.Services.Stores;
using BTCPayServer.Services.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBitcoin;
using NBitcoin.DataEncoders;

namespace BTCPayServer.Lightning.Phoenixd.Controllers
{
    public class LightningPaymentViewModel
    {
        [Required]
        public string BitcoinAddress { get; set; }

        [Required]
        public long Amount { get; set; }

        [Required]
        public long Feerate { get; set; }
    }

    [Route("~/plugins/phoenixd")]
    public class PhoenixdController : Controller
    {
        private readonly PhoenixdClient _phoenixdClient;

        public PhoenixdController()
        {
            _phoenixdClient = PhoenixdLightningClient.PhoenixdClientInstance ?? throw new Exception("Uninitialized PhoenixdClient");
        }

        [HttpGet("send")]
        public IActionResult Send()
        {
            return View(new LightningPaymentViewModel());
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send(LightningPaymentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                await _phoenixdClient.SendPayment(model.BitcoinAddress, model.Amount, model.Feerate);
                TempData["SuccessMessage"] = $"Successful payment to {model.BitcoinAddress}";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return View(model);
            }

            return RedirectToAction(nameof(Send));
        }
    }
}
