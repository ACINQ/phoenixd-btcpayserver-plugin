[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

# ⚡Phoenixd lightning support plugin⚡

This plugin enables on-chain payments in BTCPay Server when BTCPay is already configured to use **phoenixd** as its Lightning implementation.<br>
**phoenixd** is the server equivalent of the popular [phoenix wallet](https://github.com/ACINQ/phoenix) for mobile.

## Requirements
- [BTCPay Server >= 2.1.2](https://github.com/btcpayserver)
- [phoenixd >= v0.5.0](https://github.com/ACINQ/phoenixd)

## Build for Windows 11
If .NET is not yet installed:
```shell
winget install --id Microsoft.DotNet.SDK.8 --source winget
```
To build the plugin:
```shell
git clone --recurse-submodules https://github.com/ACINQ/phoenixd-btcpayserver-plugin.git
dotnet build --configuration Release -p:DebugType=None -p:DebugSymbols=false -p:DefineConstants="RAZOR_COMPILE_ON_BUILD"
dotnet run --project ./btcpayserver/BTCPayServer.PluginPacker/BTCPayServer.PluginPacker.csproj -- ./phoenixd-btcpayserver-plugin/bin/Release/net8.0/ BTCPayServer.Plugins.Phoenixd ./plugin
```
The Phoenixd plugin should now be located in `./plugin/BTCPayServer.Plugins.Phoenixd/<VERSION>/`

## Install
Install the plugin through BTCPay Server web interface Plugins > Manage Plugins > Upload Plugin (locate your `BTCPayServer.Plugins.Phoenixd.btcpay`) and restart.<br><br>
Once the Phoenixd plugin is successfully loaded, no further configuration is required. You should now be able to send on-chain payments using your Phoenixd balance in BTCPay Server. 🚀

## FAQ
**Q: How do I send an on-chain payment from my Lightning balance?**<br>
**A:** Go to Plugins > Phoenixd Bitcoin Payment to send an on-chain payment.
