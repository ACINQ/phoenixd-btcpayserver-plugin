#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
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

public sealed class PhoenixdApiClient
{
    private readonly HttpClient _httpClient;

    public PhoenixdApiClient()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("BTCPAY_BTCLIGHTNING")
            ?? throw new InvalidOperationException(
                "BTCPAY_BTCLIGHTNING is not configured.");

        var config = connectionString
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(
                x => x[0].Trim(),
                x => x[1].Trim(),
                StringComparer.OrdinalIgnoreCase);

        if (!config.TryGetValue("type", out var type) ||
            !string.Equals(type, "phoenixd",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The internal Lightning node is not Phoenixd.");
        }

        if (!config.TryGetValue("server", out var server))
            throw new InvalidOperationException(
                "Phoenixd server is missing from BTCPAY_BTCLIGHTNING.");

        if (!config.TryGetValue("password", out var password))
            throw new InvalidOperationException(
                "Phoenixd password is missing from BTCPAY_BTCLIGHTNING.");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                server.TrimEnd('/') + "/")
        };

        var auth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(":" + password));

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", auth);
    }

    public async Task<PhoenixdBalance> GetBalance()
    {
        using var response =
            await _httpClient.GetAsync("getbalance");

        var content =
            await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<PhoenixdBalance>(
                   content,
                   new JsonSerializerOptions
                   {
                       PropertyNameCaseInsensitive = true
                   })
               ?? throw new InvalidOperationException(
                   "Invalid response from phoenixd/getbalance.");
    }

    public async Task<string> SendPayment(
        string bitcoinAddress,
        long amountSat,
        long feerateSatByte)
    {
        var form = new Dictionary<string, string>
        {
            ["address"] = bitcoinAddress,
            ["amountSat"] = amountSat.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            ["feerateSatByte"] = feerateSatByte.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
        };

        using var response =
            await _httpClient.PostAsync(
                "sendtoaddress",
                new FormUrlEncodedContent(form));

        var content =
            (await response.Content.ReadAsStringAsync()).Trim();

        response.EnsureSuccessStatusCode();

        if (!System.Text.RegularExpressions.Regex.IsMatch(
                content,
                @"^[0-9a-fA-F]{64}$"))
            throw new InvalidOperationException(content);

        return content;
    }
}

public sealed class PhoenixdBalance
{
    public long BalanceSat { get; set; }
    public long FeeCreditSat { get; set; }
}

namespace BTCPayServer.Lightning.Phoenixd.ViewComponents
{
    public class PhoenixdNavItemViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync()
        {
            var connectionString =
                Environment.GetEnvironmentVariable(
                    "BTCPAY_BTCLIGHTNING");

            var isPhoenixd =
                !string.IsNullOrEmpty(connectionString) &&
                connectionString.Contains(
                    "type=phoenixd",
                    StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(
                (IViewComponentResult)View(isPhoenixd));
        }
    }
}

namespace BTCPayServer.Lightning.Phoenixd.Controllers
{
    public class LightningPaymentViewModel
    {
        [Required]
        public string BitcoinAddress { get; set; } = string.Empty;

        [Required]
        public long Amount { get; set; }

        [Required]
        public long Feerate { get; set; }

        public long LightningBalance { get; set; }
        public long LightningFeeCredit { get; set; }
    }

    [Route("~/plugins/phoenixd")]
    [Authorize(Policy = Policies.CanModifyServerSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    public class PhoenixdController : Controller
    {
        private readonly dynamic _phoenixdClient;

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
            _phoenixdClient = new PhoenixdApiClient();
        }

        [HttpGet("send")]
        public async Task<IActionResult> Send()
        {
            return View(await UpdateBalance(new LightningPaymentViewModel()));
        }

        [HttpPost("send")]
        [ValidateAntiForgeryToken]
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
