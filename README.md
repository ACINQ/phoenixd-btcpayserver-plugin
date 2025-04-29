[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)

# ⚡Phoenixd lightning support plugin⚡

This plugin enables lightning payments in BTCPay Server through your local **phoenixd** instance.<br>
**phoenixd** is the server equivalent of the popular [phoenix wallet](https://github.com/ACINQ/phoenix) for mobile.

## Requirements
- [BTCPay Server >= 2.0.0](https://github.com/btcpayserver)
- [phoenixd >= v0.5.0](https://github.com/ACINQ/phoenixd)

## Build for Windows 11
If .NET is not yet installed:
```shell
winget install --id Microsoft.DotNet.SDK.8 --source winget
```
To build the plugin:
```shell
git clone https://github.com/btcpayserver/btcpayserver.git
git clone https://github.com/ACINQ/phoenixd-btcpayserver-plugin.git
dotnet build --configuration Release -p:DebugType=None -p:DebugSymbols=false -p:DefineConstants="RAZOR_COMPILE_ON_BUILD" btcpayserver
dotnet build --configuration Release -p:DebugType=None -p:DebugSymbols=false -p:DefineConstants="RAZOR_COMPILE_ON_BUILD" phoenixd-btcpayserver-plugin
dotnet run --project ./btcpayserver/BTCPayServer.PluginPacker/BTCPayServer.PluginPacker.csproj -- ./phoenixd-btcpayserver-plugin/bin/Release/net8.0/ BTCPayServer.Plugins.Phoenixd ./plugin
```
The Phoenixd plugin should now be located in `./plugin/BTCPayServer.Plugins.Phoenixd/<VERSION>/`

## Setup phoenixd as a Docker fragment when using btcpayserver-docker (optional)
Save YAML fragment for `phoenixd` as `btcpayserver-docker/docker-compose-generator/docker-fragments/phoenixd.yml`:
```yaml
version: "3"
services:
  phoenixd:
    image: acinq/phoenixd:latest
    container_name: phoenixd
    restart: unless-stopped
    networks:
      - default
    command: ["--chain=${NBITCOIN_NETWORK:-regtest}"]
    expose:
      - "9740"
    depends_on:
      - btcpayserver
    volumes:
      - "phoenixd_datadir:/phoenix"
volumes:
  phoenixd_datadir:
exclusive:
  - lightning
```
Build docker fragment for `phoenixd`:
```shell
cd btcpayserver-docker/
./btcpay-down.sh
sudo su -
export BTCPAYGEN_ADDITIONAL_FRAGMENTS="$BTCPAYGEN_ADDITIONAL_FRAGMENTS;phoenixd"
. ./btcpay-setup.sh -i
```
⚠️ **Warning**: Make sure to save the `phoenixd` seed in a safe place after the Docker container is running:
```shell
docker exec -it phoenixd cat .phoenix/seed.dat > backup_seed.dat
```
You can also restore a previous seed by overwriting this file and restarting BTCPay Server (optional):
```shell
docker exec -i phoenixd tee .phoenix/seed.dat < backup_seed.dat > /dev/null
cd btcpayserver-docker/
./btcpay-restart.sh
```

## Install
1. Install the plugin through BTCPay Server web interface Plugins > Manage Plugins > Upload Plugin (locate your `BTCPayServer.Plugins.Phoenixd.btcpay`) and restart
2. Configure the plugin in Wallets > Lightning > Connection configuration for your custom Lightning node:
```
type=phoenixd;server=http://<IP>:<PORT>/;password=<PASSWORD>
```
Replace `<IP>:<PORT>` with your phoenixd server's address. When running BTCPay Server in Docker with the phoenixd fragment, use `phoenixd:9740` as the host and port (default).<br>
Replace `<PASSWORD>` with the `http-password` generated in your `~/.phoenix/phoenix.conf` or `.phoenix/phoenix.conf` file (with the phoenixd fragment).

Once the Phoenixd plugin is successfully loaded, no further configuration is required. You should now be able to receive lightning payments (through Phoenixd) in BTCPay Server. 🚀

## FAQ
**Q: How do I send an on-chain payment from my Lightning balance?**<br>
**A:** Go to Plugins > Phoenixd Bitcoin Payment to send an on-chain payment.

**Q: I get the error `Error while connecting to the API: Connection refused` when using `Test connection`.**<br>
**A:** The provided IP or port is invalid. Ensure that Phoenixd is running and IP/port is accessible (especially if BTCPay Server is running inside a Docker container).

**Q: I get the error `Error while connecting to the API: Invalid authentication (use basic auth with the http password set in phoenix.conf)` when using `Test connection`.**<br>
**A:** This error is usually due to an invalid password for Phoenixd. Check the `http-password` generated in your `~/.phoenix/phoenix.conf` file.
