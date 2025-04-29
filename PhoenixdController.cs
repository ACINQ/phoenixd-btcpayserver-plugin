using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
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

namespace BTCPayServer.Lightning.Phoenixd.ViewComponents
{
    public class PhoenixdNavItemViewComponent : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            bool isInitialized = PhoenixdLightningClient.PhoenixdClientInstance != null;
            return View(isInitialized);
        }
    }
}

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

        public long LightningBalance { get; set; }
        public long LightningFeeCredit { get; set; }
    }

    [Route("~/plugins/phoenixd")]
    public class PhoenixdController : Controller
    {
        private readonly PhoenixdClient _phoenixdClient;

        private async Task<LightningPaymentViewModel> UpdateBalance(LightningPaymentViewModel model)
        {
            try
            {
                var balance = await _phoenixdClient.GetBalance();
                model.LightningBalance = balance.balanceSat;
                model.LightningFeeCredit = balance.feeCreditSat;
            }
            catch (Exception)
            {
                model.LightningBalance = 0;
                model.LightningFeeCredit = 0;
            }
            return model;
        }

        public PhoenixdController()
        {
            _phoenixdClient = PhoenixdLightningClient.PhoenixdClientInstance ?? throw new Exception("Uninitialized PhoenixdClient");
        }

        [HttpGet("send")]
        public async Task<IActionResult> Send()
        {
            return View(await UpdateBalance(new LightningPaymentViewModel()));
        }

        [HttpPost("send")]
        public async Task<IActionResult> Send(LightningPaymentViewModel model)
        {
            if (!ModelState.IsValid)
                return View(await UpdateBalance(model));

            try
            {
                string TransactionId = await _phoenixdClient.SendPayment(model.BitcoinAddress, model.Amount, model.Feerate);
                if (Regex.IsMatch(TransactionId, @"^[0-9a-fA-F]{64}$"))
                {
                    TempData["SuccessMessage"] = $"Successful payment to <strong>{model.BitcoinAddress}</strong> with txid=<strong>{TransactionId}</strong>";
                }
                else
                {
                    throw new Exception(TransactionId);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error: {ex.Message}");
                return View(await UpdateBalance(model));
            }

            return RedirectToAction(nameof(Send));
        }
    }
}
